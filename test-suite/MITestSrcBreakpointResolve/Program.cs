﻿using System;
using System.IO;

using NetcoreDbgTest;
using NetcoreDbgTest.MI;
using NetcoreDbgTest.Script;

namespace NetcoreDbgTest.Script
{
    class Context
    {
        public void Prepare(string caller_trace)
        {
            Assert.Equal(MIResultClass.Done,
                         MIDebugger.Request("-file-exec-and-symbols " + ControlInfo.CorerunPath).Class,
                         @"__FILE__:__LINE__"+"\n"+caller_trace);

            Assert.Equal(MIResultClass.Done,
                         MIDebugger.Request("-exec-arguments " + ControlInfo.TargetAssemblyPath).Class,
                         @"__FILE__:__LINE__"+"\n"+caller_trace);

            Assert.Equal(MIResultClass.Running,
                         MIDebugger.Request("-exec-run").Class,
                         @"__FILE__:__LINE__"+"\n"+caller_trace);
        }

        bool IsStoppedEvent(MIOutOfBandRecord record)
        {
            if (record.Type != MIOutOfBandRecordType.Async) {
                return false;
            }

            var asyncRecord = (MIAsyncRecord)record;

            if (asyncRecord.Class != MIAsyncRecordClass.Exec ||
                asyncRecord.Output.Class != MIAsyncOutputClass.Stopped) {
                return false;
            }

            return true;
        }

        public void WasEntryPointHit(string caller_trace)
        {
            Func<MIOutOfBandRecord, bool> filter = (record) => {
                if (!IsStoppedEvent(record)) {
                    return false;
                }

                var output = ((MIAsyncRecord)record).Output;
                var reason = (MIConst)output["reason"];

                if (reason.CString != "entry-point-hit") {
                    return false;
                }

                var frame = (MITuple)output["frame"];
                var func = (MIConst)frame["func"];
                if (func.CString == ControlInfo.TestName + ".Program.Main()") {
                    return true;
                }

                return false;
            };

            Assert.True(MIDebugger.IsEventReceived(filter), @"__FILE__:__LINE__"+"\n"+caller_trace);
        }

        public void WasBreakpointHit(string caller_trace, string bpName)
        {
            var bp = (LineBreakpoint)ControlInfo.Breakpoints[bpName];

            Func<MIOutOfBandRecord, bool> filter = (record) => {
                if (!IsStoppedEvent(record)) {
                    return false;
                }

                var output = ((MIAsyncRecord)record).Output;
                var reason = (MIConst)output["reason"];

                if (reason.CString != "breakpoint-hit") {
                    return false;
                }

                var frame = (MITuple)output["frame"];
                var fileName = (MIConst)frame["file"];
                var line = ((MIConst)frame["line"]).Int;

                if (fileName.CString == bp.FileName &&
                    line == bp.NumLine) {
                    return true;
                }

                return false;
            };

            Assert.True(MIDebugger.IsEventReceived(filter),
                        @"__FILE__:__LINE__"+"\n"+caller_trace);
        }

        public void WasExit(string caller_trace)
        {
            Func<MIOutOfBandRecord, bool> filter = (record) => {
                if (!IsStoppedEvent(record)) {
                    return false;
                }

                var output = ((MIAsyncRecord)record).Output;
                var reason = (MIConst)output["reason"];

                if (reason.CString != "exited") {
                    return false;
                }

                var exitCode = (MIConst)output["exit-code"];

                if (exitCode.CString == "0") {
                    return true;
                }

                return false;
            };

            Assert.True(MIDebugger.IsEventReceived(filter), @"__FILE__:__LINE__"+"\n"+caller_trace);
        }

        public void DebuggerExit(string caller_trace)
        {
            Assert.Equal(MIResultClass.Exit,
                         MIDebugger.Request("-gdb-exit").Class,
                         @"__FILE__:__LINE__"+"\n"+caller_trace);
        }

        public string EnableBreakpoint(string caller_trace, string bpName, string bpPath = null)
        {
            Breakpoint bp = ControlInfo.Breakpoints[bpName];

            Assert.Equal(BreakpointType.Line, bp.Type, @"__FILE__:__LINE__"+"\n"+caller_trace);

            var lbp = (LineBreakpoint)bp;

            var BpResp =  MIDebugger.Request("-break-insert -f " + (bpPath != null ? bpPath : lbp.FileName)  + ":" + lbp.NumLine);

            Assert.Equal(MIResultClass.Done, BpResp.Class, @"__FILE__:__LINE__"+"\n"+caller_trace);

            CurrentBpId++;

            // return breakpoint id
            return ((MIConst)((MITuple)BpResp["bkpt"])["number"]).CString;
        }

        public void ManualEnableBreakpoint(string caller_trace, string bp_fileName, int bp_line)
        {
            var BpResp =  MIDebugger.Request("-break-insert -f " + bp_fileName  + ":" + bp_line.ToString());

            Assert.Equal(MIResultClass.Done, BpResp.Class, @"__FILE__:__LINE__"+"\n"+caller_trace);
        }

        public void WasManualBreakpointHit(string caller_trace, string bp_fileName, int bp_line)
        {
            Func<MIOutOfBandRecord, bool> filter = (record) => {
                if (!IsStoppedEvent(record)) {
                    return false;
                }

                var output = ((MIAsyncRecord)record).Output;
                var reason = (MIConst)output["reason"];

                if (reason.CString != "breakpoint-hit") {
                    return false;
                }

                var frame = (MITuple)output["frame"];
                var fileName = (MIConst)frame["file"];
                var line = ((MIConst)frame["line"]).Int;

                if (fileName.CString == bp_fileName &&
                    line == bp_line) {
                    return true;
                }

                return false;
            };

            Assert.True(MIDebugger.IsEventReceived(filter),
                        @"__FILE__:__LINE__"+"\n"+caller_trace);
        }

        public void DeleteBreakpoint(string caller_trace, string id)
        {
            Assert.Equal(MIResultClass.Done,
                         MIDebugger.Request("-break-delete " + id).Class,
                         @"__FILE__:__LINE__"+"\n"+caller_trace);
        }

        public void Continue(string caller_trace)
        {
            Assert.Equal(MIResultClass.Running,
                         MIDebugger.Request("-exec-continue").Class,
                         @"__FILE__:__LINE__"+"\n"+caller_trace);
        }

        public Context(ControlInfo controlInfo, NetcoreDbgTestCore.DebuggerClient debuggerClient)
        {
            ControlInfo = controlInfo;
            MIDebugger = new MIDebugger(debuggerClient);
        }

        ControlInfo ControlInfo;
        public MIDebugger MIDebugger { get; private set; }
        public int CurrentBpId = 0;
        public string id_bp5;
        public string id_bp5_b;
        public string id_bp6;
        public string id_bp6_b;
    }
}

namespace MITestSrcBreakpointResolve
{
    class test_constructors
    {
        int test_field = 5; // bp here! make sure you correct code (test constructor)!

        public test_constructors()
        {
            int i = 5;     // bp here! make sure you correct code (test constructor)!
        }

        public test_constructors(int i)
        {
            int j = 5;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Label.Checkpoint("init", "bp_test1", (Object context) => {
                Context Context = (Context)context;
                // setup breakpoints before process start
                // in this way we will check breakpoint resolve routine during module load

                var id1 = Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp0_delete_test1");
                var id2 = Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp0_delete_test2");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp1");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp2", "../Program.cs");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp3", "MITestSrcBreakpointResolve/Program.cs");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp4", "./MITestSrcBreakpointResolve/folder/../Program.cs");

                Context.DeleteBreakpoint(@"__FILE__:__LINE__", id1);

                Context.Prepare(@"__FILE__:__LINE__");
                Context.WasEntryPointHit(@"__FILE__:__LINE__");

                Context.DeleteBreakpoint(@"__FILE__:__LINE__", id2);

                Context.Continue(@"__FILE__:__LINE__");
            });

Label.Breakpoint("bp0_delete_test1");
Label.Breakpoint("bp0_delete_test2");
Label.Breakpoint("bp1");
Label.Breakpoint("bp2");
Label.Breakpoint("bp3");
Label.Breakpoint("resolved_bp1");       Console.WriteLine(
                                                          "Hello World!");          Label.Breakpoint("bp4");

            Label.Checkpoint("bp_test1", "bp_test2", (Object context) => {
                Context Context = (Context)context;
                // check, that actually we have only one active breakpoint per line
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "resolved_bp1");

                // check, that we have proper breakpoint ids (check, that for MI/GDB resolved breakpoints were not re-created hiddenly with different id)
                var id7 = Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp0_delete_test1"); // previously was deleted with id1
                Assert.Equal(Context.CurrentBpId.ToString(), id7, @"__FILE__:__LINE__");
                Context.DeleteBreakpoint(@"__FILE__:__LINE__", id7);

                Context.id_bp5_b = Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp5_resolve_wrong_source", "../wrong_folder/./Program.cs");
                Assert.Equal(Context.CurrentBpId.ToString(), Context.id_bp5_b, @"__FILE__:__LINE__");

                Context.id_bp5 = Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp5");
                Assert.Equal(Context.CurrentBpId.ToString(), Context.id_bp5, @"__FILE__:__LINE__");

                Context.Continue(@"__FILE__:__LINE__");
            });

Label.Breakpoint("bp5_resolve_wrong_source"); // Console.WriteLine("Hello World!");
                                        /* Console.WriteLine("Hello World!"); */
                                        Console.WriteLine("Hello World!");

Label.Breakpoint("bp5");                // Console.WriteLine("Hello World!");
                                        /* Console.WriteLine("Hello World!"); */
Label.Breakpoint("resolved_bp2");       Console.WriteLine("Hello World!");

            Label.Checkpoint("bp_test2", "bp_test3", (Object context) => {
                Context Context = (Context)context;
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "resolved_bp2");

                Context.DeleteBreakpoint(@"__FILE__:__LINE__", Context.id_bp5);
                Context.DeleteBreakpoint(@"__FILE__:__LINE__", Context.id_bp5_b);

                Context.id_bp6_b = Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp6_resolve_wrong_source", "./wrong_folder/Program.cs");
                Assert.Equal(Context.CurrentBpId.ToString(), Context.id_bp6_b, @"__FILE__:__LINE__");
    
                Context.id_bp6 = Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp6");
                Assert.Equal(Context.CurrentBpId.ToString(), Context.id_bp6, @"__FILE__:__LINE__");

                Context.Continue(@"__FILE__:__LINE__");
            });

                                        Console.WriteLine(
                                                          "Hello World!");          Label.Breakpoint("bp6_resolve_wrong_source");
Label.Breakpoint("resolved_bp3");       Console.WriteLine(
                                                          "Hello World!");          Label.Breakpoint("bp6");

            Label.Checkpoint("bp_test3", "bp_test4", (Object context) => {
                Context Context = (Context)context;
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "resolved_bp3");

                Context.DeleteBreakpoint(@"__FILE__:__LINE__", Context.id_bp6);
                Context.DeleteBreakpoint(@"__FILE__:__LINE__", Context.id_bp6_b);

                Context.EnableBreakpoint(@"__FILE__:__LINE__", "resolved_bp4");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp7", "Program.cs");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp8", "MITestSrcBreakpointResolve/Program.cs");
                var current_bp_id = Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp9", "./MITestSrcBreakpointResolve/folder/../Program.cs");

                // one more check, that we have proper breakpoint ids for MI/GDB
                Assert.Equal(Context.CurrentBpId.ToString(), current_bp_id, @"__FILE__:__LINE__");

                Context.Continue(@"__FILE__:__LINE__");
            });

Label.Breakpoint("bp7");
Label.Breakpoint("bp8");
Label.Breakpoint("resolved_bp4");       Console.WriteLine(
                                                          "Hello World!");          Label.Breakpoint("bp9");

            Label.Checkpoint("bp_test4", "bp_test_nested", (Object context) => {
                Context Context = (Context)context;
                // check, that actually we have only one active breakpoint per line
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "resolved_bp4");

                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp10");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp11");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp12");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp13");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp14");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp15");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp16");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp17");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp18");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp19");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp20");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp21");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp22");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp23");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp24");
                Context.EnableBreakpoint(@"__FILE__:__LINE__", "bp25");
                Context.Continue(@"__FILE__:__LINE__");
            });

            MITestSrcBreakpointResolve2.Program.testfunc();

            // tests resolve for nested methods
                                                                                    Label.Breakpoint("bp10");
            void nested_func1()
            {                                                                       Label.Breakpoint("resloved_bp10");
                Console.WriteLine("Hello World!");                                  
                                                                                    Label.Breakpoint("bp11");
            }                                                                       Label.Breakpoint("resloved_bp11");
            nested_func1();
                                                                                    Label.Breakpoint("bp12");
            void nested_func2()
            {                                                                       Label.Breakpoint("resloved_bp12");
                Console.WriteLine("Hello World!");                                  Label.Breakpoint("bp13");
            }
            nested_func2();
                                                                                    Label.Breakpoint("bp14");
            Console.WriteLine("Hello World!");                                      Label.Breakpoint("resloved_bp14");

            void nested_func3()
            {
                Console.WriteLine("Hello World!");                                  Label.Breakpoint("bp15");
            }
            nested_func3();

            void nested_func4() { }; void nested_func5() { };                       Label.Breakpoint("bp16");
            nested_func4();

            void nested_func6() {
            }; void nested_func7() { };                                             Label.Breakpoint("bp17");
            nested_func6();

            void nested_func8() { }; void nested_func9() {
            }; void nested_func10() { };                                            Label.Breakpoint("bp18");
            nested_func9();

            void nested_func11() { void nested_func12() { void nested_func13() { 
                                                                                    Label.Breakpoint("bp19");
            };                                                                      Label.Breakpoint("resloved_bp19");
            nested_func13(); }; 
            nested_func12(); };
            nested_func11();

            Console.WriteLine("1111Hello World!"); void nested_func14() {           Label.Breakpoint("bp20");
            Console.WriteLine("2222Hello World!");
            };                                                                      Label.Breakpoint("bp22");
            nested_func14();                                                        Label.Breakpoint("bp21");

            Label.Checkpoint("bp_test_nested", "bp_test_constructor", (Object context) => {
                Context Context = (Context)context;
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "resloved_bp10");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "resloved_bp11");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "resloved_bp12");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "bp13");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "resloved_bp14");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "bp15");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "bp16");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "bp17");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "bp18");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "resloved_bp19");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "bp20");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "bp21");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "bp22");
                Context.Continue(@"__FILE__:__LINE__");
            });

            // test constructor

            int bp = 23;                                                            Label.Breakpoint("bp23");
            test_constructors test_constr1 = new test_constructors();
            test_constructors test_constr2 = new test_constructors(5);

            Label.Checkpoint("bp_test_constructor", "bp_test_not_ordered_line_num", (Object context) => {
                Context Context = (Context)context;
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "bp23");

                Context.ManualEnableBreakpoint(@"__FILE__:__LINE__", "Program.cs", 223); // line number with "int test_field = 5;" code
                Context.ManualEnableBreakpoint(@"__FILE__:__LINE__", "Program.cs", 227); // line number with "int i = 5;" code
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasManualBreakpointHit(@"__FILE__:__LINE__", "Program.cs", 223); // line number with "int test_field = 5;" code
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasManualBreakpointHit(@"__FILE__:__LINE__", "Program.cs", 227); // line number with "int i = 5;" code
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasManualBreakpointHit(@"__FILE__:__LINE__", "Program.cs", 223); // line number with "int test_field = 5;" code
                Context.Continue(@"__FILE__:__LINE__");
            });

            // test code with sequence points that not ordered by line numbers

            Label.Breakpoint("bp24"); while(true)
            {
                break;                                                              Label.Breakpoint("bp25");
            }

            Label.Checkpoint("bp_test_not_ordered_line_num", "finish", (Object context) => {
                Context Context = (Context)context;
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "bp24");
                Context.Continue(@"__FILE__:__LINE__");
                Context.WasBreakpointHit(@"__FILE__:__LINE__", "bp25");
                Context.Continue(@"__FILE__:__LINE__");
            });

            Label.Checkpoint("finish", "", (Object context) => {
                Context Context = (Context)context;
                Context.WasExit(@"__FILE__:__LINE__");
                Context.DebuggerExit(@"__FILE__:__LINE__");
            });
        }
    }
}
