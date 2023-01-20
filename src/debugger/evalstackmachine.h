// Copyright (c) 2021 Samsung Electronics Co., LTD
// Distributed under the MIT License.
// See the LICENSE file in the project root for more information.

#pragma once

#include "cor.h"
#include "cordebug.h"

#include <string>
#include <memory>
#include <vector>
#include <list>
#include <unordered_map>
#include "interfaces/types.h"
#include "utils/torelease.h"
#include "debugger/evaluator.h"

namespace netcoredbg
{

class EvalHelpers;
class EvalWaiter;

struct EvalStackEntry
{
    enum class ResetLiteralStatus
    {
        No  = 0,
        Yes = 1
    };

    // Unresolved identifiers.
    // Note, in case we already have some resolved identifiers (iCorValue), unresolved identifiers must be resolved within iCorValue.
    std::vector<std::string> identifiers;
    // Resolved to value identifiers.
    ToRelease<ICorDebugValue> iCorValue;
    // Predefined types values
    ToRelease<ICorDebugValue> iCorValuePredefined;
    // Prevent future binding in case of conditional access with nulled object (`a?.b`, `a?[1]`, ...).
    // Note, this state could be related to iCorValue only (iCorValue must be checked for null first).
    bool preventBinding;
    // This is literal entry (value was created from literal).
    bool literal;
    // This entry is real variable (not literal, not result of expression calculation, not result of function call, ...).
    bool editable;
    // In case iCorValue is editable and property, we need extra data in order to set value.
    // Note, this data directly connected with `iCorValue` and could be available only in case `editable` is true.
    std::unique_ptr<Evaluator::SetterData> setterData;

    EvalStackEntry() : preventBinding(false), literal(false), editable(false)
    {}

    void ResetEntry(ResetLiteralStatus resetLiteral = ResetLiteralStatus::Yes)
    {
        identifiers.clear();
        iCorValue.Free();
        iCorValuePredefined.Free();
        preventBinding = false;
        if (resetLiteral == ResetLiteralStatus::Yes)
            literal = false;
        editable = false;
        setterData.reset();
    }
};

struct EvalData
{
    ICorDebugThread *pThread;
    Evaluator *pEvaluator;
    EvalHelpers *pEvalHelpers;
    EvalWaiter *pEvalWaiter;
    // In case of NumericLiteralExpression with Decimal, NewParameterizedObjectNoConstructor() are used.
    // Proper ICorDebugClass must be provided for Decimal (will be found during FindPredefinedTypes() call).
    ToRelease<ICorDebugClass> iCorDecimalClass;
    // In case eval return void, we are forced to create System.Void value.
    ToRelease<ICorDebugClass> iCorVoidClass;
    std::unordered_map<CorElementType, ToRelease<ICorDebugClass>> corElementToValueClassMap;
    FrameLevel frameLevel;
    int evalFlags;

    EvalData() :
        pThread(nullptr), pEvaluator(nullptr), pEvalHelpers(nullptr), pEvalWaiter(nullptr), evalFlags(defaultEvalFlags)
    {}
};

class EvalStackMachine
{
    std::shared_ptr<Evaluator> m_sharedEvaluator;
    std::shared_ptr<EvalHelpers> m_sharedEvalHelpers;
    std::shared_ptr<EvalWaiter> m_sharedEvalWaiter;
    EvalData m_evalData;

    // Run stack machine for particular expression.
    HRESULT Run(ICorDebugThread *pThread, FrameLevel frameLevel, int evalFlags, const std::string &expression,
                std::list<EvalStackEntry> &evalStack, std::string &output);

public:

    void SetupEval(std::shared_ptr<Evaluator> &sharedEvaluator, std::shared_ptr<EvalHelpers> &sharedEvalHelpers, std::shared_ptr<EvalWaiter> &sharedEvalWaiter)
    {
        m_sharedEvaluator = sharedEvaluator;
        m_sharedEvalHelpers = sharedEvalHelpers;
        m_sharedEvalWaiter = sharedEvalWaiter;
        m_evalData.pEvaluator = m_sharedEvaluator.get();
        m_evalData.pEvalHelpers = m_sharedEvalHelpers.get();
        m_evalData.pEvalWaiter = m_sharedEvalWaiter.get();
    }

    // Evaluate expression. Optional, return `editable` state and in case result is property - setter related information.
    HRESULT EvaluateExpression(ICorDebugThread *pThread, FrameLevel frameLevel, int evalFlags, const std::string &expression, ICorDebugValue **ppResultValue,
                               std::string &output, bool *editable = nullptr, std::unique_ptr<Evaluator::SetterData> *resultSetterData = nullptr);

    // Set value in pValue by expression with implicitly cast expression result to pValue type, if need.
    HRESULT SetValueByExpression(ICorDebugThread *pThread, FrameLevel frameLevel, int evalFlags, ICorDebugValue *pValue,
                                 const std::string &expression, std::string &output);

    // Find ICorDebugClass objects for all predefined types we need for stack machine during Private.CoreLib load.
    // See ManagedCallback::LoadModule().
    HRESULT FindPredefinedTypes(ICorDebugModule *pModule);

};

} // namespace netcoredbg
