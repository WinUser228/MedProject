using HospitalStatistics.Core.Entities;

namespace MedProject.Mapper;

public static class ReportEntryMapper
{
    public static ReportEntry ToReportEntry(ListPatientMovements src)
    {
        return new ReportEntry
        {
            BedId = src.BedId,
            ReportDate = src.ReportDate,

            PatientsAtStartOfDay = src.PatientsAtStartOfDay,
            TotalAdmitted = src.TotalAdmitted,
            FromVillage = src.FromVillage,
            ChildrenUnder14 = src.ChildrenUnder14,
            TransferredFromOtherDepartments = src.TransferredFromOtherDepartments,
            TransferredToOtherDepartments = src.TransferredToOtherDepartments,
            TotalDischarged = src.TotalDischarged,
            DischargedByTransfer = src.DischargedByTransfer,
            Died = src.Died,
            FromVillagePatient = src.FromVillagePatient,
            MothersWithPatients = src.MothersWithPatients,
            MothersDischarged = src.MothersDischarged,
        };
    }

    public static ListPatientMovements ToMovement(ReportEntry src)
    {
        return new ListPatientMovements
        {
            ReportDate = src.ReportDate,
            BedId = src.BedId,

            PatientsAtStartOfDay = src.PatientsAtStartOfDay,
            TotalAdmitted = src.TotalAdmitted,
            FromVillage = src.FromVillage,
            ChildrenUnder14 = src.ChildrenUnder14,
            TransferredFromOtherDepartments = src.TransferredFromOtherDepartments,
            TransferredToOtherDepartments = src.TransferredToOtherDepartments,
            TotalDischarged = src.TotalDischarged,
            DischargedByTransfer = src.DischargedByTransfer,
            Died = src.Died,
            TotalPatients = src.TotalPatients,
            FromVillagePatient = src.FromVillagePatient,
            MothersWithPatients = src.MothersWithPatients,
            MothersDischarged = src.MothersDischarged,
        };
    }
}

