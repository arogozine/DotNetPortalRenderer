namespace SoftwareRendererModels;

[AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
public class FixedStateAttribute : Attribute, IFixedState;
