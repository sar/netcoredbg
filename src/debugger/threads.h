// Copyright (c) 2021 Samsung Electronics Co., LTD
// Distributed under the MIT License.
// See the LICENSE file in the project root for more information.
#pragma once

#include "cor.h"
#include "cordebug.h"

#include <set>
#include <vector>
#include "interfaces/types.h"
#include "utils/rwlock.h"

namespace netcoredbg
{

class Evaluator;
ThreadId getThreadId(ICorDebugThread *pThread);

class Threads
{
    Utility::RWLock m_userThreadsRWLock;
    std::set<ThreadId> m_userThreads;
    ThreadId MainThread;
    std::shared_ptr<Evaluator> m_sharedEvaluator;

public:

    void Add(const ThreadId &threadId);
    void Remove(const ThreadId &threadId);
    HRESULT GetThreadsWithState(ICorDebugProcess *pProcess, std::vector<Thread> &threads);
    HRESULT GetThreadIds(std::vector<ThreadId> &threads);
    std::string GetThreadName(ICorDebugProcess *pProcess, const ThreadId &userThread);
    void SetEvaluator(std::shared_ptr<Evaluator> &sharedEvaluator);
};

} // namespace netcoredbg
