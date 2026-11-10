using Xunit.Abstractions;
using Xunit.Sdk;

namespace Laboratorios.Tests.Practica3.Soporte;

/// <summary>Ejecuta los casos de una clase en orden alfabético de su nombre (I5_01, I5_02, …): los pasos dependen del anterior.</summary>
public sealed class OrdenAlfabetico : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase =>
        testCases.OrderBy(t => t.TestMethod.Method.Name, StringComparer.Ordinal);
}
