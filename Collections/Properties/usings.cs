#if NET9_0_OR_GREATER
global using SyncObject = global::System.Threading.Lock;
#else
global using SyncObject = global::Ecng.Common.SyncObject;
#endif