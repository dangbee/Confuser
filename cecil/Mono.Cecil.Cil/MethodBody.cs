//
// MethodBody.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Mono.Collections.Generic;
using System.Collections.Generic;

namespace Mono.Cecil.Cil {

	public sealed class MethodBody : IVariableDefinitionProvider {

		readonly internal MethodDefinition method;

        internal ParameterDefinition this_parameter;
        internal bool preserve_max_stack;
		internal int max_stack_size;
		internal int code_size;
		internal bool init_locals;
		internal MetadataToken local_var_token;

        public byte[] method_code;
        public byte[] exception_code;
        public byte[] local_vars_code;
        public byte[] method_signature;

		internal InstructionCollection instructions;
		internal Collection<ExceptionHandler> exceptions;
		internal Collection<VariableDefinition> variables;
		Scope scope;

		public MethodDefinition Method {
			get { return method; }
		}

		public bool PreserveMaxStackSize {
            get { return preserve_max_stack; }
            set { preserve_max_stack = value; }
		}

		public int MaxStackSize {
			get { return max_stack_size; }
			set { max_stack_size = value; }
		}

		public int CodeSize {
			get { return code_size; }
		}

		public bool InitLocals {
			get { return init_locals; }
			set { init_locals = value; }
		}

		public MetadataToken LocalVarToken {
			get { return local_var_token; }
			set { local_var_token = value; }
		}

		public Collection<Instruction> Instructions {
			get {
				if (instructions == null)
					instructions = new InstructionCollection ();

				return instructions;
			}
		}

		public bool HasExceptionHandlers {
			get { return !exceptions.IsNullOrEmpty (); }
		}

		public Collection<ExceptionHandler> ExceptionHandlers {
			get {
				if (exceptions == null)
					exceptions = new Collection<ExceptionHandler> ();

				return exceptions;
			}
		}

		public bool HasVariables {
			get { return !variables.IsNullOrEmpty (); }
		}

		public Collection<VariableDefinition> Variables {
			get {
				if (variables == null)
					variables = new VariableDefinitionCollection ();

				return variables;
			}
		}

		public Scope Scope {
			get { return scope; }
			set { scope = value; }
		}

		public ParameterDefinition ThisParameter {
			get {
				if (method == null || method.DeclaringType == null)
					throw new NotSupportedException ();

				if (this_parameter == null)
					this_parameter = new ParameterDefinition ("0", ParameterAttributes.None, method.DeclaringType);

				return this_parameter;
			}
		}

		public MethodBody (MethodDefinition method)
		{
			this.method = method;
		}

		public ILProcessor GetILProcessor ()
		{
			return new ILProcessor (this);
		}

        public void ComputeHeader()
        {
            CodeWriter.ComputeHeader(this);
        }

        public void ComputeOffsets()
        {
            instructions.ComputeOffsets();
        }
	}

	public interface IVariableDefinitionProvider {
		bool HasVariables { get; }
		Collection<VariableDefinition> Variables { get; }
	}

	class VariableDefinitionCollection : Collection<VariableDefinition> {

        public byte[] signature;
		internal VariableDefinitionCollection ()
		{
		}

		internal VariableDefinitionCollection (int capacity)
			: base (capacity)
		{
		}

		protected override void OnAdd (VariableDefinition item, int index)
		{
			item.index = index;
		}

		protected override void OnInsert (VariableDefinition item, int index)
		{
			item.index = index;

			for (int i = index; i < size; i++)
				items [i].index = i + 1;
		}

		protected override void OnSet (VariableDefinition item, int index)
		{
			item.index = index;
		}

		protected override void OnRemove (VariableDefinition item, int index)
		{
			item.index = -1;

			for (int i = index + 1; i < size; i++)
				items [i].index = i - 1;
		}
	}

	class InstructionCollection : Collection<Instruction> {

		internal InstructionCollection ()
		{
		}

		internal InstructionCollection (int capacity)
			: base (capacity)
		{
		}

        internal bool initing = false;
        internal List<Instruction> instReferences = new List<Instruction>();

        protected override void OnAdd(Instruction item, int index)
        {
            if (item.opcode.OperandType == OperandType.InlineBrTarget ||
                item.opcode.OperandType == OperandType.ShortInlineBrTarget ||
                item.opcode.OperandType == OperandType.InlineSwitch)
                instReferences.Add(item);

            if (index == 0)
                return;

            var previous = items[index - 1];
            previous.next = item;
            item.previous = previous;
        }

		protected override void OnInsert (Instruction item, int index)
		{
			if (size == 0)
				return;

			var current = items [index];
			if (current == null) {
				var last = items [index - 1];
				last.next = item;
				item.previous = last;
				return;
			}

			var previous = current.previous;
			if (previous != null) {
				previous.next = item;
				item.previous = previous;
			}

			current.previous = item;
            item.next = current;

            if (item.opcode.OperandType == OperandType.InlineBrTarget ||
                item.opcode.OperandType == OperandType.ShortInlineBrTarget ||
                item.opcode.OperandType == OperandType.InlineSwitch)
                instReferences.Add(item);
		}

        protected override void OnSet(Instruction item, int index)
		{
			var current = items [index];

			item.previous = current.previous;
			item.next = current.next;

			current.previous = null;
            current.next = null;

            if (item.opcode.OperandType == OperandType.InlineBrTarget ||
                item.opcode.OperandType == OperandType.ShortInlineBrTarget ||
                item.opcode.OperandType == OperandType.InlineSwitch)
                instReferences.Add(item);

            if (current.opcode.OperandType == OperandType.InlineBrTarget ||
                current.opcode.OperandType == OperandType.ShortInlineBrTarget ||
                current.opcode.OperandType == OperandType.InlineSwitch)
                instReferences.Remove(item);
		}

		protected override void OnRemove (Instruction item, int index)
		{
			var previous = item.previous;
			if (previous != null)
				previous.next = item.next;

			var next = item.next;
			if (next != null)
				next.previous = item.previous;

			item.previous = null;
			item.next = null;

            if (item.opcode.OperandType == OperandType.InlineBrTarget ||
                item.opcode.OperandType == OperandType.ShortInlineBrTarget ||
                item.opcode.OperandType == OperandType.InlineSwitch)
                instReferences.Remove(item);
		}

        internal void ComputeOffsets()
        {
            if (!initing)
            {
                int offset = 0;
                for (int i = 0; i < this.Count; i++)
                {
                    this[i].index = i;
                    this[i].offset = offset;
                    this[i].previous = (i == 0 ? null : this[i - 1]);
                    this[i].next = (i == this.Count - 1 ? null : this[i + 1]);
                    offset += this[i].GetSize();
                }
            }
        }
	}
}
