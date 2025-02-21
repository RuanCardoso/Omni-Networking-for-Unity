# Database System

!!! note "Modified Database Libraries"
    Omni Networking uses customized versions of [SQLKata](https://sqlkata.com/docs) and [Dapper](https://github.com/DapperLib/Dapper) modified for Unity:

    **Improved:**

    - ✨ Reduced memory allocations and garbage collector pressure
    - 🔄 Async/await support with Task/ValueTask/UniTask
    - ⚡ Added support for IL2CPP compilation

    These modifications ensure better performance and compatibility within Unity's ecosystem while maintaining the original libraries' ease of use.

    !!! warning "IL2CPP Compatibility"
        Some database features may have limited compatibility when using IL2CPP compilation. However, all essential functionality remains available through alternative approaches supported by the Omni Networking framework.