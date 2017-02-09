using System;

namespace Golden.Attributes
{
    /// <summary>
    /// Maps a class to a type.
    /// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class ExportAttribute : Attribute
    {
        private readonly Type _ContractType;

        public Type ContractType
        {
            get { return _ContractType; }
        }

        public ExportAttribute() : this((Type)null)
        {
        }
        public ExportAttribute(Type contractType)
        {
            _ContractType = contractType;
        }
    }
}
