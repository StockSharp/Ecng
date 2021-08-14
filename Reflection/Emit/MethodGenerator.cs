namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Reflection.Emit;

	using Ecng.Collections;
	using Ecng.Common;

	#endregion

	public class MethodGenerator : BaseGenerator<MethodBase>
	{
		#region Private Fields

		private readonly ILGenerator _generator;

		#endregion

		#region MethodGenerator.ctor()

		internal MethodGenerator(TypeGenerator typeGenerator, ConstructorBuilder builder)
			: this(typeGenerator, builder.GetILGenerator(), builder)
		{
		}

		internal MethodGenerator(TypeGenerator typeGenerator, MethodBuilder builder)
			: this(typeGenerator, builder.GetILGenerator(), builder)
		{
		}

		public MethodGenerator(DynamicMethod builder)
			: this(null, builder.GetILGenerator(), builder)
		{
		}

		internal MethodGenerator(TypeGenerator typeGenerator, ILGenerator generator, MethodBase method)
			: base(method)
		{
			TypeGenerator = typeGenerator;
			_generator = generator;
		}

		#endregion

		public TypeGenerator TypeGenerator { get; }

		#region Locals

		private readonly List<LocalGenerator> _locals = new List<LocalGenerator>();

		public IEnumerable<LocalGenerator> Locals => _locals;

		#endregion

		#region Parameters

		private readonly List<ParameterGenerator> _parameters = new();

		public IEnumerable<ParameterGenerator> Parameters => _parameters;

		#endregion

		#region CreateGenericParameters

		public IEnumerable<GenericArgGenerator> CreateGenericParameters(params string[] names)
		{
			var generators = new List<GenericArgGenerator>();

			foreach (var builder in ((MethodBuilder)Builder).DefineGenericParameters(names))
				generators.Add(new GenericArgGenerator(builder));

			return generators;
		}

		#endregion

		#region CreateLocal

		public LocalGenerator CreateLocal(Type type)
		{
			return CreateLocal(type, false);
		}

		public LocalGenerator CreateLocal(Type type, bool isPinned)
		{
			var local = new LocalGenerator(_generator.DeclareLocal(type, isPinned));
			_locals.Add(local);
			return local;
		}

		#endregion

		#region CreateParameter

		public ParameterGenerator CreateParameter(string name)
		{
			return CreateParameter(name, ParameterAttributes.None);
		}

		public ParameterGenerator CreateParameter(string name, ParameterAttributes attrs)
		{
			ParameterBuilder builder;

			if (Builder is ConstructorBuilder)
				builder = Builder.To<ConstructorBuilder>().DefineParameter(_parameters.Count + 1, attrs, name);
			else if (Builder is MethodBuilder)
				builder = Builder.To<MethodBuilder>().DefineParameter(_parameters.Count + 1, attrs, name);
			else if (Builder is DynamicMethod)
				builder = Builder.To<DynamicMethod>().DefineParameter(_parameters.Count + 1, attrs, name);
			else
				throw new InvalidOperationException();

			// store null for track params count
			var parameter = builder is null ? null : new ParameterGenerator(builder);
			_parameters.Add(parameter);
			return parameter;
		}

		#endregion

		#region Declaration API

		public Label DefineLabel()
		{
			return _generator.DefineLabel();
		}

		public MethodGenerator MarkLabel(Label label)
		{
			_generator.MarkLabel(label);
			return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Beq"/>, label) that
		/// transfers control to a target instruction if two values are equal.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Beq">OpCodes.Beq</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator beq(Label label)
		{
			_generator.Emit(OpCodes.Beq, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Beq_S"/>, label) that
		/// transfers control to a target instruction (short form) if two values are equal.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Beq_S">OpCodes.Beq_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator beq_s(Label label)
		{
			_generator.Emit(OpCodes.Beq_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Bge"/>, label) that
		/// transfers control to a target instruction if the first value is greater than or equal to the second value.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Bge">OpCodes.Bge</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator bge(Label label)
		{
			_generator.Emit(OpCodes.Bge, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Bge_S"/>, label) that
		/// transfers control to a target instruction (short form) 
		/// if the first value is greater than or equal to the second value.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Bge_S">OpCodes.Bge_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator bge_s(Label label)
		{
			_generator.Emit(OpCodes.Bge_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Bge_Un"/>, label) that
		/// transfers control to a target instruction if the the first value is greather than the second value,
		/// when comparing unsigned integer values or unordered float values.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Bge_Un">OpCodes.Bge_Un</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator bge_un(Label label)
		{
			_generator.Emit(OpCodes.Bge_Un, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Bge_Un_S"/>, label) that
		/// transfers control to a target instruction (short form) if if the the first value is greather than the second value,
		/// when comparing unsigned integer values or unordered float values.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Bge_Un_S">OpCodes.Bge_Un_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator bge_un_s(Label label)
		{
			_generator.Emit(OpCodes.Bge_Un_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Bgt"/>, label) that
		/// transfers control to a target instruction if the first value is greater than the second value.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Bgt">OpCodes.Bgt</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator bgt(Label label)
		{
			_generator.Emit(OpCodes.Bgt, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Bgt_S"/>, label) that
		/// transfers control to a target instruction (short form) if the first value is greater than the second value.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Bgt_S">OpCodes.Bgt_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator bgt_s(Label label)
		{
			_generator.Emit(OpCodes.Bgt_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Bgt_Un"/>, label) that
		/// transfers control to a target instruction if the first value is greater than the second value,
		/// when comparing unsigned integer values or unordered float values.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Bgt_Un">OpCodes.Bgt_Un</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator bgt_un(Label label)
		{
			_generator.Emit(OpCodes.Bgt_Un, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Bgt_Un_S"/>, label) that
		/// transfers control to a target instruction (short form) if the first value is greater than the second value,
		/// when comparing unsigned integer values or unordered float values.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Bgt_Un_S">OpCodes.Bgt_Un_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator bgt_un_s(Label label)
		{
			_generator.Emit(OpCodes.Bgt_Un_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Ble"/>, label) that
		/// transfers control to a target instruction if the first value is less than or equal to the second value.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Ble">OpCodes.Ble</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator ble(Label label)
		{
			_generator.Emit(OpCodes.Ble, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Ble_S"/>, label) that
		/// transfers control to a target instruction (short form) if the first value is less than or equal to the second value.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Ble_S">OpCodes.Ble_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator ble_s(Label label)
		{
			_generator.Emit(OpCodes.Ble_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Ble_Un"/>, label) that
		/// transfers control to a target instruction if the first value is less than or equal to the second value,
		/// when comparing unsigned integer values or unordered float values.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Ble_Un">OpCodes.Ble_Un</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator ble_un(Label label)
		{
			_generator.Emit(OpCodes.Ble_Un, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Ble_Un_S"/>, label) that
		/// transfers control to a target instruction (short form) if the first value is less than or equal to the second value,
		/// when comparing unsigned integer values or unordered float values.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Ble_Un_S">OpCodes.Ble_Un_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator ble_un_s(Label label)
		{
			_generator.Emit(OpCodes.Ble_Un_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Blt"/>, label) that
		/// transfers control to a target instruction if the first value is less than the second value.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Blt">OpCodes.Blt</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator blt(Label label)
		{
			_generator.Emit(OpCodes.Blt, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Blt_S"/>, label) that
		/// transfers control to a target instruction (short form) if the first value is less than the second value.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Blt_S">OpCodes.Blt_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator blt_s(Label label)
		{
			_generator.Emit(OpCodes.Blt_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Blt_Un"/>, label) that
		/// transfers control to a target instruction if the first value is less than the second value,
		/// when comparing unsigned integer values or unordered float values.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Blt_Un">OpCodes.Blt_Un</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator blt_un(Label label)
		{
			_generator.Emit(OpCodes.Blt_Un, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Blt_Un_S"/>, label) that
		/// transfers control to a target instruction (short form) if the first value is less than the second value,
		/// when comparing unsigned integer values or unordered float values.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Blt_Un_S">OpCodes.Blt_Un_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator blt_un_s(Label label)
		{
			_generator.Emit(OpCodes.Blt_Un_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Bne_Un"/>, label) that
		/// transfers control to a target instruction when two unsigned integer values or unordered float values are not equal.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Bne_Un">OpCodes.Bne_Un</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator bne_un(Label label)
		{
			_generator.Emit(OpCodes.Bne_Un, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Bne_Un_S"/>, label) that
		/// transfers control to a target instruction (short form) 
		/// when two unsigned integer values or unordered float values are not equal.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Bne_Un_S">OpCodes.Bne_Un_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator bne_un_s(Label label)
		{
			_generator.Emit(OpCodes.Bne_Un_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Br"/>, label) that
		/// unconditionally transfers control to a target instruction. 
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Br">OpCodes.Br</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator br(Label label)
		{
			_generator.Emit(OpCodes.Br, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Brfalse"/>, label) that
		/// transfers control to a target instruction if value is false, a null reference (Nothing in Visual Basic), or zero.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Brfalse">OpCodes.Brfalse</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator brfalse(Label label)
		{
			_generator.Emit(OpCodes.Brfalse, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Brfalse_S"/>, label) that
		/// transfers control to a target instruction if value is false, a null reference, or zero. 
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Brfalse_S">OpCodes.Brfalse_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator brfalse_s(Label label)
		{
			_generator.Emit(OpCodes.Brfalse_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Brtrue"/>, label) that
		/// transfers control to a target instruction if value is true, not null, or non-zero.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Brtrue">OpCodes.Brtrue</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator brtrue(Label label)
		{
			_generator.Emit(OpCodes.Brtrue, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Brtrue_S"/>, label) that
		/// transfers control to a target instruction (short form) if value is true, not null, or non-zero.
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Brtrue_S">OpCodes.Brtrue_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator brtrue_s(Label label)
		{
			_generator.Emit(OpCodes.Brtrue_S, label); return this;
		}

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Br_S"/>, label) that
		/// unconditionally transfers control to a target instruction (short form).
		/// </summary>
		/// <param name="label">The label to branch from this location.</param>
		/// <seealso cref="OpCodes.Br_S">OpCodes.Br_S</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode,Label)">ILGenerator.Emit</seealso>
		public MethodGenerator br_s(Label label)
		{
			_generator.Emit(OpCodes.Br_S, label); return this;
		}

		public MethodGenerator leave(Label label) { _generator.Emit(OpCodes.Leave, label); return this; }
		public MethodGenerator leave_s(Label label) { _generator.Emit(OpCodes.Leave_S, label); return this; }

		#endregion

		#region Exception API

		public MethodGenerator @try() { _generator.BeginExceptionBlock(); return this; }
		public MethodGenerator end_try() { _generator.EndExceptionBlock(); return this; }

		public MethodGenerator @catch() { return @catch(typeof(Exception)); }
		public MethodGenerator @catch(Type exceptionType) { _generator.BeginCatchBlock(exceptionType); return this; }

		public MethodGenerator @finally() { _generator.BeginFinallyBlock(); return this; }
		public MethodGenerator end_finally() { _generator.Emit(OpCodes.Endfinally); return this; }

		public MethodGenerator filter() { _generator.BeginExceptFilterBlock(); return this; }
		public MethodGenerator endfilter() { _generator.Emit(OpCodes.Endfilter); return this; }

		public MethodGenerator fault() { _generator.BeginFaultBlock(); return this; }

		public MethodGenerator @throw(Type exception)
		{
			return newobj(exception)
						.@throw();
		}

		public MethodGenerator @throw()
		{
			_generator.Emit(OpCodes.Throw);
			return this;
		}

		public MethodGenerator rethrow() { _generator.Emit(OpCodes.Rethrow); return this; }

		#endregion

		#region New & Init API

		public MethodGenerator newarr(Type type) { _generator.Emit(OpCodes.Newarr, type); return this; }
		public MethodGenerator newobj(ConstructorInfo ctor) { _generator.Emit(OpCodes.Newobj, ctor); return this; }
		public MethodGenerator newobj(Type type, params Type[] parameters) { return newobj(type.GetMember<ConstructorInfo>(parameters)); }
		public MethodGenerator initobj(Type type) { _generator.Emit(OpCodes.Initobj, type); return this; }
		public MethodGenerator initblk() { _generator.Emit(OpCodes.Initblk); return this; }

		public MethodGenerator CreateObj(ConstructorInfo ctor)
		{
			if (ctor.DeclaringType.IsValueType)
				return initobj(ctor.DeclaringType);
			else
				return newobj(ctor);
		}

		public MethodGenerator CreateObj(Type type)
		{
			if (type.IsValueType)
				return initobj(type);
			else
				return newobj(type.GetConstructor(Type.EmptyTypes));
		}

		#endregion

		#region Casting & Boxing API

		/*
		public MethodGenerator UnboxAnyIfValueType(Type type)
		{
			if (type.IsValueType)
				_generator.Emit(OpCodes.Unbox_Any, type);

			return this;
		}
		*/

		public MethodGenerator unbox_any(Type type) { _generator.Emit(OpCodes.Unbox_Any, type); return this; }

		public MethodGenerator castclass(Type objectType) { _generator.Emit(OpCodes.Castclass, objectType); return this; }
		public MethodGenerator unbox(Type type) { _generator.Emit(OpCodes.Unbox, type); return this; }
		public MethodGenerator box(Type type) { _generator.Emit(OpCodes.Box, type); return this; }

		public MethodGenerator Cast(Type type)
		{
			return type.IsValueType ? unbox_any(type) : castclass(type);
		}

		public MethodGenerator BoxIfValueType(Type type)
		{
			return type.IsValueType ? box(type) : this;
		}

		/*
		public MethodGenerator CastType(Type type)
		{
			if (type.IsValueType)
			{
				unbox(type);
				ldobj(type);
			}
			else
				castclass(type);

			return this;
		}
		*/

		#endregion

		#region Converting API

		public MethodGenerator conv_i() { _generator.Emit(OpCodes.Conv_I); return this; }
		public MethodGenerator conv_i1() { _generator.Emit(OpCodes.Conv_I1); return this; }
		public MethodGenerator conv_i2() { _generator.Emit(OpCodes.Conv_I2); return this; }
		public MethodGenerator conv_i4() { _generator.Emit(OpCodes.Conv_I4); return this; }
		public MethodGenerator conv_i8() { _generator.Emit(OpCodes.Conv_I8); return this; }

		public MethodGenerator conv_ovf_i() { _generator.Emit(OpCodes.Conv_Ovf_I); return this; }
		public MethodGenerator conv_ovf_i1() { _generator.Emit(OpCodes.Conv_Ovf_I1); return this; }
		public MethodGenerator conv_ovf_i2() { _generator.Emit(OpCodes.Conv_Ovf_I2); return this; }
		public MethodGenerator conv_ovf_i4() { _generator.Emit(OpCodes.Conv_Ovf_I4); return this; }
		public MethodGenerator conv_ovf_i8() { _generator.Emit(OpCodes.Conv_Ovf_I8); return this; }

		public MethodGenerator conv_ovf_i_un() { _generator.Emit(OpCodes.Conv_Ovf_I_Un); return this; }
		public MethodGenerator conv_ovf_i1_un() { _generator.Emit(OpCodes.Conv_Ovf_I1_Un); return this; }
		public MethodGenerator conv_ovf_i2_un() { _generator.Emit(OpCodes.Conv_Ovf_I2_Un); return this; }
		public MethodGenerator conv_ovf_i4_un() { _generator.Emit(OpCodes.Conv_Ovf_I4_Un); return this; }
		public MethodGenerator conv_ovf_i8_un() { _generator.Emit(OpCodes.Conv_Ovf_I8_Un); return this; }

		public MethodGenerator conv_ovf_u() { _generator.Emit(OpCodes.Conv_Ovf_U); return this; }
		public MethodGenerator conv_ovf_u1() { _generator.Emit(OpCodes.Conv_Ovf_U1); return this; }
		public MethodGenerator conv_ovf_u2() { _generator.Emit(OpCodes.Conv_Ovf_U2); return this; }
		public MethodGenerator conv_ovf_u4() { _generator.Emit(OpCodes.Conv_Ovf_U4); return this; }
		public MethodGenerator conv_ovf_u8() { _generator.Emit(OpCodes.Conv_Ovf_U8); return this; }

		public MethodGenerator conv_ovf_u_un() { _generator.Emit(OpCodes.Conv_Ovf_U_Un); return this; }
		public MethodGenerator conv_ovf_u1_un() { _generator.Emit(OpCodes.Conv_Ovf_U1_Un); return this; }
		public MethodGenerator conv_ovf_u2_un() { _generator.Emit(OpCodes.Conv_Ovf_U2_Un); return this; }
		public MethodGenerator conv_ovf_u4_un() { _generator.Emit(OpCodes.Conv_Ovf_U4_Un); return this; }
		public MethodGenerator conv_ovf_u8_un() { _generator.Emit(OpCodes.Conv_Ovf_U8_Un); return this; }

		public MethodGenerator conv_u() { _generator.Emit(OpCodes.Conv_U); return this; }
		public MethodGenerator conv_u1() { _generator.Emit(OpCodes.Conv_U1); return this; }
		public MethodGenerator conv_u2() { _generator.Emit(OpCodes.Conv_U2); return this; }
		public MethodGenerator conv_u4() { _generator.Emit(OpCodes.Conv_U4); return this; }
		public MethodGenerator conv_u8() { _generator.Emit(OpCodes.Conv_U8); return this; }

		public MethodGenerator conv_r4() { _generator.Emit(OpCodes.Conv_R4); return this; }
		public MethodGenerator conv_r8() { _generator.Emit(OpCodes.Conv_R8); return this; }
		public MethodGenerator conv_r_un() { _generator.Emit(OpCodes.Conv_R_Un); return this; }

		#endregion

		#region Is Instance API

		public MethodGenerator isinst(Type type) { _generator.Emit(OpCodes.Isinst, type); return this; }

		#endregion

		#region Load API

		public MethodGenerator Load(Type type)
		{
			if (type.IsPrimitive)
			{
				if (type == typeof(float))
					return ldc_r4(0);
				else if (type == typeof(float))
					return ldc_r8(0);
				else if (type == typeof(long) || type == typeof(ulong))
				{
					return ldc_i4_0()
								.conv_i8();
				}
				else
					return ldc_i4_0();
			}
			else if (type.IsStruct())
			{
				if (type == typeof(decimal))
				{
					return ldc_i4_0()
							.newobj(type, typeof(int));
				}
				else
					return initobj(type);
			}
			else if (type.IsEnum())
				return Load(type.GetEnumBaseType());
			else if (type.IsClass)
				return ldnull();
			else
				throw new ArgumentOutOfRangeException(nameof(type), type.To<string>());
		}

		public MethodGenerator ldlen() { _generator.Emit(OpCodes.Ldlen); return this; }
		public MethodGenerator ldlen(int count)
		{
			ldlen();
			_generator.Emit(OpCodes.Ldc_I4, count);
			return this;
		}

		public MethodGenerator ldnull() { _generator.Emit(OpCodes.Ldnull); return this; }

		private MethodGenerator ldloc_0() { _generator.Emit(OpCodes.Ldloc_0); return this; }
		private MethodGenerator ldloc_1() { _generator.Emit(OpCodes.Ldloc_1); return this; }
		private MethodGenerator ldloc_2() { _generator.Emit(OpCodes.Ldloc_2); return this; }
		private MethodGenerator ldloc_3() { _generator.Emit(OpCodes.Ldloc_3); return this; }

		private MethodGenerator ldloc(int index)
		{
			switch (index)
			{
				case 0: return ldloc_0();
				case 1: return ldloc_1();
				case 2: return ldloc_2();
				case 3: return ldloc_3();
				default:
					if (index <= byte.MaxValue)
						_generator.Emit(OpCodes.Ldloc_S, (byte)index);
					else
						_generator.Emit(OpCodes.Ldloc, (short)index);
					
					return this;
			}
		}

		public MethodGenerator ldloc(LocalGenerator local)
		{
			if (local is null)
				throw new ArgumentNullException(nameof(local));

			return ldloc(local.Builder.LocalIndex);
		}

		private MethodGenerator ldloca(int index)
		{
			if (index <= byte.MaxValue)
				_generator.Emit(OpCodes.Ldloca_S, (byte)index);
			else
				_generator.Emit(OpCodes.Ldloca, (short)index);

			return this;
		}

		public MethodGenerator ldloca(LocalGenerator local)
		{
			if (local is null)
				throw new ArgumentNullException(nameof(local));

			return ldloca(local.Builder.LocalIndex);
		}

		public MethodGenerator ldsfld(FieldInfo field) { _generator.Emit(OpCodes.Ldsfld, field); return this; }
		public MethodGenerator ldsfld(FieldGenerator field)
		{
			if (field is null)
				throw new ArgumentNullException(nameof(field));

			return ldsfld(field.Builder);
		}
		public MethodGenerator ldsflda(FieldInfo field) { _generator.Emit(OpCodes.Ldsflda, field); return this; }
		public MethodGenerator ldsflda(FieldGenerator field)
		{
			if (field is null)
				throw new ArgumentNullException(nameof(field));

			return ldsflda(field.Builder);
		}

		/*
		public MethodGenerator LoadField(FieldInfo field)
		{
			if (field.FieldType.IsValueType)
				return ldflda(field);
			else
				return ldfld(field);
		}

		public MethodGenerator LoadStaticField(FieldInfo field)
		{
			if (field.FieldType.IsValueType)
				return ldsflda(field);
			else
				return ldsfld(field);
		}
		*/

		public MethodGenerator LoadArg(Type type, byte index)
		{
			if (type.IsValueType)
				return ldarga_s(index);
			else
				return ldarg_s(index);
		}

		public MethodGenerator ldarg_0() { _generator.Emit(OpCodes.Ldarg_0); return this; }
		public MethodGenerator ldarg_1() { _generator.Emit(OpCodes.Ldarg_1); return this; }
		public MethodGenerator ldarg_2() { _generator.Emit(OpCodes.Ldarg_2); return this; }
		public MethodGenerator ldarg_3() { _generator.Emit(OpCodes.Ldarg_3); return this; }

		public MethodGenerator ldarg_s(byte index)
		{
			switch (index)
			{
				case 0: return ldarg_0();
				case 1: return ldarg_1();
				case 2: return ldarg_2();
				case 3: return ldarg_3();
				default:
					_generator.Emit(OpCodes.Ldarg_S, index);
					return this;
			}
		}

		public MethodGenerator ldarga_s(byte index) { _generator.Emit(OpCodes.Ldarga_S, index); return this; }

		public MethodGenerator ldc_i4_0() { _generator.Emit(OpCodes.Ldc_I4_0); return this; }
		public MethodGenerator ldc_i4_1() { _generator.Emit(OpCodes.Ldc_I4_1); return this; }
		public MethodGenerator ldc_i4_2() { _generator.Emit(OpCodes.Ldc_I4_2); return this; }
		public MethodGenerator ldc_i4_3() { _generator.Emit(OpCodes.Ldc_I4_3); return this; }

		public MethodGenerator ldc_i4(int count)
		{
			if (count >= byte.MinValue && count <= byte.MaxValue)
				return ldc_i4_s((byte)count);
			else
			{
				_generator.Emit(OpCodes.Ldc_I4, count);
				return this;
			}
		}


		public MethodGenerator ldc_i4_s(byte value)
		{
			switch (value)
			{
				case 0: return ldc_i4_0();
				case 1: return ldc_i4_1();
				case 2: return ldc_i4_2();
				case 3: return ldc_i4_3();
				default:
					_generator.Emit(OpCodes.Ldc_I4_S, value);
					return this;
			}
		}

		public MethodGenerator ldfld(FieldInfo field) { _generator.Emit(OpCodes.Ldfld, field); return this; }
		public MethodGenerator ldfld(FieldGenerator field)
		{
			if (field is null)
				throw new ArgumentNullException(nameof(field));

			return ldfld(field.Builder);
		}
		public MethodGenerator ldfld(Type type, string fieldName) { return ldfld(type.GetField(fieldName)); }
		public MethodGenerator ldflda(FieldInfo field) { _generator.Emit(OpCodes.Ldflda, field); return this; }
		public MethodGenerator ldflda(FieldGenerator field)
		{
			if (field is null)
				throw new ArgumentNullException(nameof(field));

			return ldflda(field.Builder);
		}
		public MethodGenerator ldflda(Type type, string fieldName) { return ldfld(type.GetField(fieldName)); }

		public MethodGenerator ldtoken(Type type) { _generator.Emit(OpCodes.Ldtoken, type); return this; }
		public MethodGenerator ldtoken(MethodInfo method) { _generator.Emit(OpCodes.Ldtoken, method); return this; }

		public MethodGenerator ldstr(string str) { _generator.Emit(OpCodes.Ldstr, str); return this; }

		public MethodGenerator ldelem_ref() { _generator.Emit(OpCodes.Ldelem_Ref); return this; }

		public MethodGenerator ldc_i8(long value) { _generator.Emit(OpCodes.Ldc_I8, value); return this; }
#if !SILVERLIGHT
		[CLSCompliant(false)]
#endif
		public MethodGenerator ldc_i8(ulong value) { _generator.Emit(OpCodes.Ldc_I8, value); return this; }
		public MethodGenerator ldc_r4(float value) { _generator.Emit(OpCodes.Ldc_R4, value); return this; }
		public MethodGenerator ldc_r8(double value) { _generator.Emit(OpCodes.Ldc_R8, value); return this; }

		public MethodGenerator ldind_u1() { _generator.Emit(OpCodes.Ldind_U1); return this; }
		public MethodGenerator ldind_u2() { _generator.Emit(OpCodes.Ldind_U2); return this; }
		public MethodGenerator ldind_u4() { _generator.Emit(OpCodes.Ldind_U4); return this; }
		public MethodGenerator ldind_i1() { _generator.Emit(OpCodes.Ldind_I1); return this; }
		public MethodGenerator ldind_i2() { _generator.Emit(OpCodes.Ldind_I2); return this; }
		public MethodGenerator ldind_i4() { _generator.Emit(OpCodes.Ldind_I4); return this; }
		public MethodGenerator ldind_i8() { _generator.Emit(OpCodes.Ldind_I8); return this; }
		public MethodGenerator ldind_r4() { _generator.Emit(OpCodes.Ldind_R4); return this; }
		public MethodGenerator ldind_r8() { _generator.Emit(OpCodes.Ldind_R8); return this; }
		public MethodGenerator ldind_ref() { _generator.Emit(OpCodes.Ldind_Ref); return this; }
		public MethodGenerator ldobj(Type type) { _generator.Emit(OpCodes.Ldobj, type); return this; }

		public MethodGenerator LoadObj(Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (type.IsEnum())
				type = type.GetEnumBaseType();

			if (type.IsValueType)
			{
				if (type == typeof(byte))
					return ldind_u1();
				else if (type == typeof(ushort))
					return ldind_u2();
				else if (type == typeof(uint))
					return ldind_u4();
				else if (type == typeof(ulong))
					return ldind_i8();
				else if (type == typeof(sbyte))
					return ldind_i1();
				else if (type == typeof(short))
					return ldind_i2();
				else if (type == typeof(int))
					return ldind_i4();
				else if (type == typeof(long))
					return ldind_i8();
				else if (type == typeof(float))
					return ldind_r4();
				else if (type == typeof(double))
					return ldind_r8();
				else
					return ldobj(type);
			}
			else
				return ldind_ref();
		}

		#endregion

		#region St (pop) API

		private MethodGenerator stloc_0() { _generator.Emit(OpCodes.Stloc_0); return this; }
		private MethodGenerator stloc_1() { _generator.Emit(OpCodes.Stloc_1); return this; }
		private MethodGenerator stloc_2() { _generator.Emit(OpCodes.Stloc_2); return this; }
		private MethodGenerator stloc_3() { _generator.Emit(OpCodes.Stloc_3); return this; }

		private MethodGenerator stloc(int index)
		{
			switch (index)
			{
				case 0: return stloc_0();
				case 1: return stloc_1();
				case 2: return stloc_2();
				case 3: return stloc_3();
				default:
					if (index <= byte.MaxValue)
						_generator.Emit(OpCodes.Stloc_S, (byte)index);
					else
						_generator.Emit(OpCodes.Stloc, (short)index);

					return this;
			}
		}

		public MethodGenerator stloc(LocalGenerator local)
		{
			if (local is null)
				throw new ArgumentNullException(nameof(local));

			return stloc(local.Builder.LocalIndex);
		}

		public MethodGenerator stfld(FieldInfo field) { _generator.Emit(OpCodes.Stfld, field); return this; }
		public MethodGenerator stfld(FieldGenerator field)
		{
			if (field is null)
				throw new ArgumentNullException(nameof(field));

			return stfld(field.Builder);
		}
		public MethodGenerator stfld(Type type, string fieldName) { return stfld(type.GetField(fieldName)); }

		public MethodGenerator stelem_ref() { _generator.Emit(OpCodes.Stelem_Ref); return this; }

		public MethodGenerator stsfld(FieldInfo field) { _generator.Emit(OpCodes.Stsfld, field); return this; }
		public MethodGenerator stsfld(FieldGenerator field)
		{
			if (field is null)
				throw new ArgumentNullException(nameof(field));

			return stsfld(field.Builder);
		}

		public MethodGenerator stind_ref() { _generator.Emit(OpCodes.Stind_Ref); return this; }

		#endregion

		#region Call Method API

		public MethodGenerator GetMember(bool returnFromStack, MemberInfo member)
		{
			var isStatic = member.IsStatic();

			switch (member.MemberType)
			{
				case MemberTypes.Field:
					if (!returnFromStack && member.GetMemberType().IsValueType)
					{
						return isStatic ? ldsflda((FieldInfo)member) : ldflda((FieldInfo)member);
					}
					else
					{
						return isStatic ? ldsfld((FieldInfo)member) : ldfld((FieldInfo)member);
					}
				case MemberTypes.Property:
					var method = ((PropertyInfo)member).GetGetMethod(true);

					if (method is null)
						throw new ArgumentException("Get accessor for property '{0}' of type '{1}' isn't exist.".Put(member.Name, member.ReflectedType.Name), nameof(member));

					if (isStatic || member.DeclaringType.IsValueType)
						return call(method);
					else
						return callvirt(method);
				default:
					throw new ArgumentOutOfRangeException(nameof(member), member.MemberType.To<string>());
			}
		}

		public MethodGenerator SetMember(MemberInfo member)
		{
			var isStatic = member.IsStatic();

			switch (member.MemberType)
			{
				case MemberTypes.Field:
					return isStatic ? stsfld((FieldInfo)member) : stfld((FieldInfo)member);
				case MemberTypes.Property:
					var method = ((PropertyInfo)member).GetSetMethod(true);

					if (method is null)
						throw new ArgumentException("Set accessor for property '{0}' of type '{1}' doesn't exist.".Put(member.Name, member.ReflectedType.Name), nameof(member));

					if (isStatic || member.DeclaringType.IsValueType)
						return call(method);
					else
						return callvirt(method);
				default:
					throw new ArgumentOutOfRangeException(nameof(member), member.MemberType.To<string>());
			}
		}

		public MethodGenerator CallMethod(MethodInfo method)
		{
			if (method.IsStatic || method.IsFinal)
				return call(method);
			else
				return callvirt(method);
		}

		/*
		public MethodGenerator call(MethodInfo method)
        {
			return call(method, null);
        }

		public MethodGenerator call(MethodInfo method, params Type[] optionalParameterTypes)
        {
			_generator.EmitCall(OpCodes.Call, method, optionalParameterTypes);
            return this;
        }
		*/

		public MethodGenerator call(Type type, string methodName, BindingFlags bindingAttr, Type[] additionalTypes)
		{
			return call(type.GetMember<MethodInfo>(methodName, bindingAttr, additionalTypes));
		}

		public MethodGenerator call(Type type, string methodName, BindingFlags bindingAttr)
		{
			return call(type, methodName, bindingAttr, null);
		}

		public MethodGenerator call(Type type, string methodName, params Type[] additionalTypes)
		{
			return call(type.GetMember<MethodInfo>(methodName, additionalTypes));
		}

		/*
		public MethodGenerator call(ConstructorInfo ctor)
        {
			_generator.Emit(OpCodes.Call, ctor);
            return this;
        }
		*/

		public MethodGenerator call(MethodBase method)
		{
			if (method is ConstructorInfo ctor)
				_generator.Emit(OpCodes.Call, ctor);
			else
				_generator.Emit(OpCodes.Call, (MethodInfo)method);

			return this;
		}

		#endregion

		#region callvirt

		public MethodGenerator callvirt(MethodInfo methodInfo, params Type[] optionalParameterTypes)
		{
			if (optionalParameterTypes != null && optionalParameterTypes.IsEmpty())
				optionalParameterTypes = null;

			_generator.EmitCall(OpCodes.Callvirt, methodInfo, optionalParameterTypes);
			return this;
		}

		public MethodGenerator callvirt(Type type, string methodName)
		{
			return callvirt(type.GetMember<MethodInfo>(methodName));
		}

		public MethodGenerator callvirt(Type type, string methodName, BindingFlags bindingAttr, params Type[] additionalTypes)
		{
			return callvirt(type.GetMember<MethodInfo>(methodName, bindingAttr, additionalTypes));
		}

		public MethodGenerator callvirt(Type type, string methodName, params Type[] additionalTypes)
		{
			return callvirt(type.GetMember<MethodInfo>(methodName, additionalTypes));
		}

		#endregion

		public MethodGenerator jmp(MethodInfo method) { _generator.Emit(OpCodes.Jmp, method); return this; }
		public MethodGenerator ceq() { _generator.Emit(OpCodes.Ceq); return this; }
		public MethodGenerator dup() { _generator.Emit(OpCodes.Dup); return this; }
		public MethodGenerator ret() { _generator.Emit(OpCodes.Ret); return this; }

		public MethodGenerator nop() { _generator.Emit(OpCodes.Nop); return this; }

		/// <summary>
		/// Calls ILGenerator.Emit(<see cref="OpCodes.Break"/>) that
		/// signals the Common Language Infrastructure (CLI) to inform the debugger that a break point has been tripped.
		/// </summary>
		/// <seealso cref="OpCodes.Break">OpCodes.Break</seealso>
		/// <seealso cref="System.Reflection.Emit.ILGenerator.Emit(OpCode)">ILGenerator.Emit</seealso>
		public MethodGenerator @break
		{
			get { _generator.Emit(OpCodes.Break); return this; }
		}

		public TDelegate CreateDelegate<TDelegate>()
		{
			return CreateDelegate<TDelegate>(null);
		}

		public TDelegate CreateDelegate<TDelegate>(object target)
		{
			var method = Builder.To<DynamicMethod>();

			Delegate @delegate = target != null ? method.CreateDelegate(typeof(TDelegate), target) : method.CreateDelegate(typeof(TDelegate));

			return @delegate.To<TDelegate>();
		}
	}
}