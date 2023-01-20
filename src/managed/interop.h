// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2017 Samsung Electronics Co., LTD
#pragma once
#include "utils/platform.h"

#include "cor.h"
#include "cordebug.h"

#include <string>
#include <vector>
#include <functional>
#include <unordered_set>


namespace netcoredbg
{

namespace Interop
{
    // 0xfeefee is a magic number for "#line hidden" directive.
    // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-line
    // https://docs.microsoft.com/en-us/archive/blogs/jmstall/line-hidden-and-0xfeefee-sequence-points
    constexpr int HiddenLine = 0xfeefee;

    struct SequencePoint {
        int32_t startLine;
        int32_t startColumn;
        int32_t endLine;
        int32_t endColumn;
        int32_t offset;
        BSTR document;
        SequencePoint() :
            startLine(0), startColumn(0),
            endLine(0), endColumn(0),
            offset(0),
            document(nullptr)
        {}
        ~SequencePoint() noexcept;

        SequencePoint(const SequencePoint&) = delete;
        SequencePoint& operator=(const SequencePoint&) = delete;
        SequencePoint(SequencePoint&& other) noexcept
            :startLine(other.startLine)
            ,startColumn(other.startColumn)
            ,endLine(other.endLine)
            ,endColumn(other.endColumn)
            ,offset(other.offset)
            ,document(other.document)
        {
            other.document = nullptr;
        }
        SequencePoint& operator=(SequencePoint&& other)
        {
            if(this == std::addressof(other))
                return *this;
            startLine = other.startLine;
            startColumn = other.startColumn;
            endLine = other.endLine;
            endColumn = other.endColumn;
            offset = other.offset;
            document = other.document;
            other.document = nullptr;
            return *this;
        }
    };

    struct AsyncAwaitInfoBlock
    {
        uint32_t yield_offset;
        uint32_t resume_offset;
        uint32_t token; // note, this is internal token number, runtime method token for module should be calculated as "mdMethodDefNil + token"
        
        AsyncAwaitInfoBlock() :
            yield_offset(0), resume_offset(0), token(0)
        {}
    };

    // WARNING! Due to CoreCLR limitations, Init() / Shutdown() sequence can be used only once during process execution.
    // Note, init in case of error will throw exception, since this is fatal for debugger (CoreCLR can't be re-init).
    void Init(const std::string &coreClrPath);
    // WARNING! Due to CoreCLR limitations, Shutdown() can't be called out of the Main() scope, for example, from global object destructor.
    void Shutdown();

    HRESULT LoadSymbolsForPortablePDB(const std::string &modulePath, BOOL isInMemory, BOOL isFileLayout, ULONG64 peAddress, ULONG64 peSize,
                                      ULONG64 inMemoryPdbAddress, ULONG64 inMemoryPdbSize, VOID **ppSymbolReaderHandle);
    void DisposeSymbols(PVOID pSymbolReaderHandle);
    HRESULT GetSequencePointByILOffset(PVOID pSymbolReaderHandle, mdMethodDef MethodToken, ULONG32 IlOffset, SequencePoint *sequencePoint);
    HRESULT GetNextUserCodeILOffset(PVOID pSymbolReaderHandle, mdMethodDef MethodToken, ULONG32 IlOffset, ULONG32 &ilNextOffset, bool *noUserCodeFound);
    HRESULT GetNamedLocalVariableAndScope(PVOID pSymbolReaderHandle, mdMethodDef methodToken, ULONG localIndex,
                                          WCHAR *localName, ULONG localNameLen, ULONG32 *pIlStart, ULONG32 *pIlEnd);
    HRESULT GetHoistedLocalScopes(PVOID pSymbolReaderHandle, mdMethodDef methodToken, PVOID *data, int32_t &hoistedLocalScopesCount);
    HRESULT GetStepRangesFromIP(PVOID pSymbolReaderHandle, ULONG32 ip, mdMethodDef MethodToken, ULONG32 *ilStartOffset, ULONG32 *ilEndOffset);
    HRESULT GetModuleMethodsRanges(PVOID pSymbolReaderHandle, uint32_t constrTokensNum, PVOID constrTokens, uint32_t normalTokensNum, PVOID normalTokens, PVOID *data);
    HRESULT ResolveBreakPoints(PVOID pSymbolReaderHandles[], int32_t tokenNum, PVOID Tokens, int32_t sourceLine, int32_t nestedToken, int32_t &Count, const std::string &sourcePath, PVOID *data);
    HRESULT GetAsyncMethodSteppingInfo(PVOID pSymbolReaderHandle, mdMethodDef methodToken, std::vector<AsyncAwaitInfoBlock> &AsyncAwaitInfo, ULONG32 *ilOffset);
    HRESULT GetSource(PVOID symbolReaderHandle, const std::string fileName, PVOID *data, int32_t *length);
    HRESULT LoadDeltaPdb(const std::string &pdbPath, VOID **ppSymbolReaderHandle, std::unordered_set<mdMethodDef> &methodTokens);
    HRESULT CalculationDelegate(PVOID firstOp, int32_t firstType, PVOID secondOp, int32_t secondType, int32_t operationType, int32_t &resultType, PVOID *data, std::string &errorText);
    HRESULT GenerateStackMachineProgram(const std::string &expr, PVOID *ppStackProgram, std::string &textOutput);
    void ReleaseStackMachineProgram(PVOID pStackProgram);
    HRESULT NextStackCommand(PVOID pStackProgram, int32_t &Command, PVOID &Ptr, std::string &textOutput);
    PVOID AllocString(const std::string &str);
    HRESULT StringToUpper(std::string &String);
    BSTR SysAllocStringLen(int32_t size);
    void SysFreeString(BSTR ptrBSTR);
    PVOID CoTaskMemAlloc(int32_t size);
    void CoTaskMemFree(PVOID ptr);
} // namespace Interop


// Set of platform-specific functions implemented in separate, platform-specific modules.
template <typename PlatformTag>
struct InteropTraits
{
    /// This function searches *.dll files in specified directory and adds full paths to files
    /// to colon-separated list `tpaList` (semicolon-separated list on Windows).
    static void AddFilesFromDirectoryToTpaList(const std::string &directory, std::string& tpaList);

    /// This function unsets `CORECLR_ENABLE_PROFILING' environment variable.
    static void UnsetCoreCLREnv();

    /// Returns the length of a BSTR.
    static UINT SysStringLen(BSTR bstrString);
};

typedef InteropTraits<PlatformTag> InteropPlatform;

} // namespace netcoredbg
