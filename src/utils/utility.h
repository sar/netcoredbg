// Copyright (c) 2020 Samsung Electronics Co., LTD
// Distributed under the MIT License.
// See the LICENSE file in the project root for more information.

// \file utility.h This file contains few supplimentar classes and functions residing in Utility namespace.

#pragma once
#include <stddef.h>

namespace netcoredbg
{

namespace Utility
{

/// @{ This function is similar to `std::size()` from C++17 and allows to determine
/// the object size as number of elements, which might be stored within object
/// (opposed to sizeof(), which returns object sizes in bytes). Typically these
/// functions applicable to arrays and to classes like std::array.
template <typename T> constexpr auto Size(const T& v) -> decltype(v.size()) { return v.size(); }
template <class T, size_t N> constexpr size_t Size(const T (&)[N]) noexcept { return N; }
/// @}

  
/// This type is similar to `std::index_sequence` from C++14 and typically
/// should be used in pair with `MakeSequence` type to create sequence of
/// indices usable in template metaprogramming techniques.
template <size_t... Index> struct Sequence {};

// Actual implementation of MakeSequence type.
namespace Internals
{
    template <size_t Size, size_t... Index> struct MakeSequence : MakeSequence<Size-1, Size-1, Index...> {};
    template <size_t... Index> struct MakeSequence<0, Index...> { typedef Sequence<Index...> type; };
}

/// MakeSequence type is similar to `std::make_index_sequence<N>` from C++14.
/// Instantination of this type exapands to `Sequence<size_t...N>` type, where
/// `N` have values from 0 to `Size-1`.
template <size_t Size> using MakeSequence = typename Internals::MakeSequence<Size>::type;


/// @{ This is similar to std:void_t which is defined in c++17.
template <typename... Args> struct MakeVoid { typedef void type; };
template <typename... Args> using Void = typename MakeVoid<Args...>::type;
/// @}


/// This template is similar to `offsetof` macros in plain C. It allows
/// to get offset of specified member in some particular class.
template <typename Owner, typename Member>
static inline constexpr size_t offset_of(const Member Owner::*mem)
{
    return reinterpret_cast<size_t>(&(reinterpret_cast<Owner*>(0)->*mem));
}

/// This template is similar to well known `container_of` macros. It allows
/// to get pointer to owner class from pointer to member.
template <typename Owner, typename Member>
static inline constexpr Owner *container_of(Member *ptr, const Member Owner::*mem)
{
    return reinterpret_cast<Owner*>(reinterpret_cast<size_t>(ptr) - offset_of(mem));
}


// This is helper class which simplifies implementation of singleton classes.
//
// Usage example:
//   1) define dictinct type of singleton: typedef Singleton<YourType> YourSingleton;
//   2) to access your singleton use expression: YourSingleton::instance().operations...
//
template <typename T> struct Singleton
{
    static T& instance()
    {
        static T val;
        return val;
    }
};

} // Utility namespace
} // namespace netcoredbg
