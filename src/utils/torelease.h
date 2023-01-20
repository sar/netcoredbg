// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2017 Samsung Electronics Co., LTD

#pragma once

#include <assert.h>
#include "utils/logger.h"

namespace netcoredbg
{

// This class acts a smart pointer which calls the Release method on any object
// you place in it when the ToRelease class falls out of scope.  You may use it
// just like you would a standard pointer to a COM object (including if (foo),
// if (!foo), if (foo == 0), etc) except for two caveats:
//     1. This class never calls AddRef and it always calls Release when it
//        goes out of scope.
//     2. You should never use & to try to get a pointer to a pointer unless
//        you call Release first, or you will leak whatever this object contains
//        prior to updating its internal pointer.
template<class T>
class ToRelease
{
public:
    ToRelease()
        : m_ptr(nullptr)
    {}

    ToRelease(T* ptr)
        : m_ptr(ptr)
    {}

    ~ToRelease()
    {
        Free();
    }

    void operator=(T *ptr)
    {
        Free();

        m_ptr = ptr;
    }

    T* operator->() const
    {
        assert(m_ptr != 0);  // accessing NULL pointer
        return m_ptr;
    }

    operator T*() const
    {
        return m_ptr;
    }

    T** operator&()
    {
        assert(m_ptr == 0);  // make sure, that previously stored value of type `T' isn't lost
        return &m_ptr;
    }

    // Special case for EvalFunction() arguments in order to avoid temporary array pointers creation code.
    // DO NOT use it, unless you know what you are doing. Operator & must be used instead.
    T** GetRef()
    {
        return &m_ptr;
    }

    T* GetPtr() const
    {
        return m_ptr;
    }

    T* Detach()
    {
        T* pT = m_ptr;
        m_ptr = nullptr;
        return pT;
    }

    void Free()
    {
        if (m_ptr != nullptr) {
            m_ptr->Release();
            m_ptr = nullptr;
        }
    }

    ToRelease(ToRelease&& that) noexcept : m_ptr(that.m_ptr) { that.m_ptr = nullptr; }
    ToRelease& operator=(ToRelease&& that)
    {
        if (m_ptr != nullptr)
            m_ptr->Release();

        m_ptr = that.m_ptr;
        that.m_ptr = nullptr;
    }
private:
    ToRelease(const ToRelease& that) = delete;
    ToRelease& operator=(const ToRelease& that) = delete;
    T* m_ptr;
};

#ifndef IfFailRet
#define IfFailRet(EXPR) do { Status = (EXPR); if(FAILED(Status)) { LOGE("%s : 0x%08x", #EXPR, Status); return (Status); } } while (0)
#endif

#ifndef _countof
#define _countof(x) (sizeof(x)/sizeof(x[0]))
#endif

#ifdef PAL_STDCPP_COMPAT
#define _iswprint   PAL_iswprint
#define _wcslen     PAL_wcslen
#define _wcsncmp    PAL_wcsncmp
#define _wcsrchr    PAL_wcsrchr
#define _wcscmp     PAL_wcscmp
#define _wcschr     PAL_wcschr
#define _wcscspn    PAL_wcscspn
#define _wcscat     PAL_wcscat
#define _wcsstr     PAL_wcsstr
#else // PAL_STDCPP_COMPAT
#define _iswprint   iswprint
#define _wcslen     wcslen
#define _wcsncmp    wcsncmp
#define _wcsrchr    wcsrchr
#define _wcscmp     wcscmp
#define _wcschr     wcschr
#define _wcscspn    wcscspn
#define _wcscat     wcscat
#define _wcsstr     wcsstr
#endif // !PAL_STDCPP_COMPAT

const int mdNameLen = 2048;

#ifdef _MSC_VER
#define PACK_BEGIN __pragma( pack(push, 1) )
#define PACK_END __pragma( pack(pop) )
#else
#define PACK_BEGIN
#define PACK_END __attribute__((packed))
#endif

} // namespace netcoredbg
