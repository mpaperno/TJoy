using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyDescription("C# Wrapper for vGenInterface.dll")]
[assembly: AssemblyCompany("Shaul Eizikovich")]
[assembly: AssemblyProduct("vGenInterfaceWrap")]
[assembly: AssemblyCopyright("Copyright Shaul Eizikovich ©  2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("441AA28A-3FB9-4523-AAA4-FF9E322BC7D2")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyTitle("vGenInterfaceWrap  - C# Wrapper [Deb]")]
#else
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyTitle("vGenInterfaceWrap  - C# Wrapper [Rel]")]
#endif
