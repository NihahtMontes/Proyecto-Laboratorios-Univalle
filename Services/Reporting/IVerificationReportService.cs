using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Services.Reporting
{
    /// <summary>
    /// Service for generating verification reports in different formats.
    /// </summary>
    public interface IVerificationReportService
    {
        /// <summary>
        /// Generates a PDF report for a specific verification.
        /// </summary>
        /// <param name="verification">The verification data.</param>
        /// <returns>Byte array containing the PDF data.</returns>
        byte[] GenerateVerificationPdf(Verification verification);

        /// <summary>
        /// Generates an Excel report for a list of verifications.
        /// </summary>
        /// <param name="verifications">The list of verifications.</param>
        /// <returns>Byte array containing the Excel data.</returns>
        byte[] GenerateVerificationsExcel(IEnumerable<Verification> verifications);

        /// <summary>
        /// Generates a laboratory equipment status report based on the official template.
        /// </summary>
        /// <param name="laboratoryName">Name or code of the laboratory</param>
        /// <param name="units">List of physical equipment units in that laboratory</param>
        /// <param name="term">Academic term (e.g., "II/2025")</param>
        /// <param name="responsible">Name of the responsible person</param>
        /// <param name="date">Date of verification</param>
        /// <returns>Byte array of the Excel file</returns>
        byte[] GenerateLaboratoryReport(string laboratoryName, IEnumerable<EquipmentUnit> units, string term, string responsible, DateTime date);
    }
}
