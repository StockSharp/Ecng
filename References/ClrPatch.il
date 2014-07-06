.assembly extern WindowsBase
{
  .ver 4:0:0:0
  .publickeytoken = (31 BF 38 56 AD 36 4E 35)
}
.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
}

.assembly ClrPatch
{
  .ver 1:0:0:0
}
.module ClrPatch.dll
.imagebase 0x11000000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001

.namespace ClrPatch
{
  .class public abstract auto ansi beforefieldinit DerivablePropertyChangedEventManager
         extends [WindowsBase]System.ComponentModel.PropertyChangedEventManager
  {
    .method family hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack  0
      IL_0000:  nop
      IL_0001:  ret
    }
  }
}