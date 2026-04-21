using MedProject.Utilities;
using System;
using System.Linq;

namespace MedProject;

public class ReportEntry : ViewModelValidationBase
{
    #region Constants

    private static class ValidationMessages
    {
        public const string PatientsExceedBeds = "Количество пациентов ({0}) не может превышать количество коек ({1}).";
        public const string FromVillageExceedsTotalAdmitted = "Поступило сельских жителей не может быть больше, чем всего поступило больных.";
        public const string ChildrenExceedsTotalAdmitted = "Поступило детей не может быть больше, чем всего поступило больных.";
        public const string DischargedByTransferExceedsTotal = "Выписано в другие стационары не может быть больше, чем выписано всего.";
        public const string MothersDischargedExceedsTotal = "Выписано матерей не может быть больше, чем выписано всего.";
        public const string MothersDischargedExceedsMothersWithPatients = "Выписано матерей ({0}) не может быть больше, чем состоит матерей ({1}).";
        public const string DiedExceedsPatients = "Умерло не может быть больше, чем общее количество пациентов ({0}).";
        public const string FromVillagePatientExceedsTotal = "Состоит сельских жителей не может быть больше, чем состоит всего.";
        public const string FromVillagePatientExceedsBeds = "Состоит сельских жителей не может превышать количество коек ({0}).";
        public const string MothersWithPatientsExceedsTotal = "Состоит матерей не может быть больше, чем состоит всего.";
        public const string MothersWithPatientsExceedsBeds = "Состоит матерей не может превышать количество коек ({0}).";
        public const string MothersExceedsChildren = "Количество матерей ({0}) не может быть больше, чем количество детей ({1}).";
        public const string PatientsAtStartExceedsBeds = "Значение 'Состояло' ({0}) не может превышать количество коек ({1}).";
        public const string PatientsAtStartLessThanDischarged = "Состояло больных на начало суток ({0}) не может быть меньше, чем выписано ({1}).";
        public const string PatientsAtStartPlusAdmittedExceedsBeds = "Сумма состояло больных на начало суток ({0}) и поступило всего ({1}) не может превышать количество коек ({2}).";
        public const string DiedExceedsPatientsAtStart = "Умерших ({0}) не может быть больше, чем состояло больных на начало суток ({1}).";
        public const string TransferredFromExceedsTotalPatients = "Переведено из других отделений ({0}) не может быть больше, чем всего пациентов ({1}).";
        public const string TransferredToExceedsTotalPatients = "Переведено в другие отделения ({0}) не может быть больше, чем всего пациентов ({1}).";
    }

    #endregion

    #region Properties

    public int BedId { get; set; }
    public DateOnly ReportDate { get; set; }

    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private int _code;
    public int Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    private int? _koikiCount;
    public int? KoikiCount
    {
        get => _koikiCount;
        set
        {
            if (SetProperty(ref _koikiCount, value))
            {
                NotifyDependentProperties(nameof(TotalPatients), nameof(FromVillagePatient),
                    nameof(MothersWithPatients), nameof(PatientsAtStartOfDay), nameof(TotalAdmitted));
                ValidateMany(
                    nameof(TotalPatients),
                    nameof(FromVillagePatient),
                    nameof(MothersWithPatients),
                    nameof(PatientsAtStartOfDay),
                    nameof(TotalAdmitted));
            }
        }
    }

    private int? _patientsAtStartOfDay;
    public int? PatientsAtStartOfDay
    {
        get => _patientsAtStartOfDay;
        set
        {
            if (SetProperty(ref _patientsAtStartOfDay, value))
            {
                NotifyDependentProperties(nameof(TotalPatients), nameof(Died), nameof(TotalDischarged),
                    nameof(TotalAdmitted), nameof(TransferredFromOtherDepartments), nameof(TransferredToOtherDepartments),
                    nameof(MothersWithPatients));
                NotifyDependentProperties(nameof(CrossRowError));

                ValidateMany(
                    nameof(PatientsAtStartOfDay),
                    nameof(TotalPatients),
                    nameof(Died),
                    nameof(TotalDischarged),
                    nameof(TotalAdmitted),
                    nameof(TransferredFromOtherDepartments),
                    nameof(TransferredToOtherDepartments),
                    nameof(MothersWithPatients));
            }
        }
    }

    private int? _totalAdmitted;
    public int? TotalAdmitted
    {
        get => _totalAdmitted;
        set
        {
            if (SetProperty(ref _totalAdmitted, value))
            {
                NotifyDependentProperties(nameof(TotalPatients), nameof(FromVillage),
                    nameof(ChildrenUnder14), nameof(Died), nameof(PatientsAtStartOfDay), nameof(MothersWithPatients));
                ValidateMany(
                    nameof(TotalAdmitted),
                    nameof(TotalPatients),
                    nameof(FromVillage),
                    nameof(ChildrenUnder14),
                    nameof(Died),
                    nameof(PatientsAtStartOfDay),
                    nameof(MothersWithPatients));
            }
        }
    }

    private int? _fromVillage;
    public int? FromVillage
    {
        get => _fromVillage;
        set
        {
            if (SetProperty(ref _fromVillage, value))
            {
                NotifyDependentProperties(nameof(TotalAdmitted));
                ValidateMany(nameof(FromVillage), nameof(TotalAdmitted));
            }
        }
    }

    private int? _childrenUnder14;
    public int? ChildrenUnder14
    {
        get => _childrenUnder14;
        set
        {
            if (SetProperty(ref _childrenUnder14, value))
            {
                NotifyDependentProperties(nameof(TotalAdmitted), nameof(MothersWithPatients));
                ValidateMany(nameof(ChildrenUnder14), nameof(TotalAdmitted), nameof(MothersWithPatients));
            }
        }
    }

    private int? _transferredFromOtherDepartments;
    public int? TransferredFromOtherDepartments
    {
        get => _transferredFromOtherDepartments;
        set
        {
            if (SetProperty(ref _transferredFromOtherDepartments, value))
            {
                NotifyDependentProperties(nameof(TotalPatients), nameof(Died), nameof(PatientsAtStartOfDay), nameof(MothersWithPatients));
                ValidateMany(
                    nameof(TransferredFromOtherDepartments),
                    nameof(TotalPatients),
                    nameof(Died),
                    nameof(PatientsAtStartOfDay),
                    nameof(MothersWithPatients));
            }
        }
    }

    private int? _totalDischarged;
    public int? TotalDischarged
    {
        get => _totalDischarged;
        set
        {
            if (SetProperty(ref _totalDischarged, value))
            {
                NotifyDependentProperties(nameof(TotalPatients), nameof(DischargedByTransfer),
                    nameof(MothersDischarged), nameof(PatientsAtStartOfDay), nameof(MothersWithPatients));
                ValidateMany(
                    nameof(TotalDischarged),
                    nameof(TotalPatients),
                    nameof(DischargedByTransfer),
                    nameof(MothersDischarged),
                    nameof(PatientsAtStartOfDay),
                    nameof(MothersWithPatients));
            }
        }
    }

    private int? _transferredToOtherDepartments;
    public int? TransferredToOtherDepartments
    {
        get => _transferredToOtherDepartments;
        set
        {
            if (SetProperty(ref _transferredToOtherDepartments, value))
            {
                NotifyDependentProperties(nameof(TotalPatients), nameof(PatientsAtStartOfDay), nameof(MothersWithPatients));
                ValidateMany(
                    nameof(TransferredToOtherDepartments),
                    nameof(TotalPatients),
                    nameof(PatientsAtStartOfDay),
                    nameof(MothersWithPatients));
            }
        }
    }

    private int? _dischargedByTransfer;
    public int? DischargedByTransfer
    {
        get => _dischargedByTransfer;
        set
        {
            if (SetProperty(ref _dischargedByTransfer, value))
            {
                NotifyDependentProperties(nameof(TotalPatients), nameof(TotalDischarged), nameof(MothersWithPatients));
                ValidateMany(
                    nameof(DischargedByTransfer),
                    nameof(TotalDischarged),
                    nameof(TotalPatients),
                    nameof(MothersWithPatients));
            }
        }
    }

    private int? _died;
    public int? Died
    {
        get => _died;
        set
        {
            if (SetProperty(ref _died, value))
            {
                NotifyDependentProperties(nameof(TotalPatients), nameof(MothersWithPatients));
                ValidateMany(nameof(Died), nameof(TotalPatients), nameof(MothersWithPatients));
            }
        }
    }

    public int TotalPatients =>
        (PatientsAtStartOfDay ?? 0)
        + (TotalAdmitted ?? 0)
        + (TransferredFromOtherDepartments ?? 0)
        - (TotalDischarged ?? 0)
        - (TransferredToOtherDepartments ?? 0)
        - (Died ?? 0);

    private int? _fromVillagePatient;
    public int? FromVillagePatient
    {
        get => _fromVillagePatient;
        set
        {
            if (SetProperty(ref _fromVillagePatient, value))
                ValidateMany(nameof(FromVillagePatient), nameof(TotalPatients));
        }
    }

    private int? _mothersWithPatients;
    public int? MothersWithPatients
    {
        get => _mothersWithPatients;
        set
        {
            if (SetProperty(ref _mothersWithPatients, value))
            {
                NotifyDependentProperties(nameof(ChildrenUnder14), nameof(MothersDischarged));
                ValidateMany(nameof(MothersWithPatients), nameof(ChildrenUnder14), nameof(MothersDischarged), nameof(TotalPatients));
            }
        }
    }

    private int? _mothersDischarged;
    public int? MothersDischarged
    {
        get => _mothersDischarged;
        set
        {
            if (SetProperty(ref _mothersDischarged, value))
            {
                NotifyDependentProperties(nameof(TotalDischarged));
                ValidateMany(nameof(MothersDischarged), nameof(TotalDischarged), nameof(MothersWithPatients));
            }
        }
    }

    private string _crossRowError;
    public string CrossRowError
    {
        get => _crossRowError;
        set
        {
            if (SetProperty(ref _crossRowError, value))
            {
                NotifyDependentProperties(nameof(PatientsAtStartOfDay));
                ValidateMany(nameof(PatientsAtStartOfDay));
            }
        }
    }

    public int? PreviousDayTotalPatients { get; set; }

    // --- Per-field error indicators (updated via PropertyChanged from SetFieldError) ---

    public bool HasPatientsAtStartOfDayError => HasPropertyError(nameof(PatientsAtStartOfDay));
    public bool HasTotalAdmittedError => HasPropertyError(nameof(TotalAdmitted));
    public bool HasFromVillageError => HasPropertyError(nameof(FromVillage));
    public bool HasChildrenUnder14Error => HasPropertyError(nameof(ChildrenUnder14));
    public bool HasTransferredFromOtherDepartmentsError => HasPropertyError(nameof(TransferredFromOtherDepartments));
    public bool HasTransferredToOtherDepartmentsError => HasPropertyError(nameof(TransferredToOtherDepartments));
    public bool HasTotalDischargedError => HasPropertyError(nameof(TotalDischarged));
    public bool HasDischargedByTransferError => HasPropertyError(nameof(DischargedByTransfer));
    public bool HasDiedError => HasPropertyError(nameof(Died));
    public bool HasFromVillagePatientError => HasPropertyError(nameof(FromVillagePatient));
    public bool HasMothersWithPatientsError => HasPropertyError(nameof(MothersWithPatients));
    public bool HasMothersDischargedError => HasPropertyError(nameof(MothersDischarged));

    public string? PatientsAtStartOfDayErrorMessage => GetPropertyErrorMessage(nameof(PatientsAtStartOfDay));
    public string? TotalAdmittedErrorMessage => GetPropertyErrorMessage(nameof(TotalAdmitted));
    public string? FromVillageErrorMessage => GetPropertyErrorMessage(nameof(FromVillage));
    public string? ChildrenUnder14ErrorMessage => GetPropertyErrorMessage(nameof(ChildrenUnder14));
    public string? TransferredFromOtherDepartmentsErrorMessage => GetPropertyErrorMessage(nameof(TransferredFromOtherDepartments));
    public string? TransferredToOtherDepartmentsErrorMessage => GetPropertyErrorMessage(nameof(TransferredToOtherDepartments));
    public string? TotalDischargedErrorMessage => GetPropertyErrorMessage(nameof(TotalDischarged));
    public string? DischargedByTransferErrorMessage => GetPropertyErrorMessage(nameof(DischargedByTransfer));
    public string? DiedErrorMessage => GetPropertyErrorMessage(nameof(Died));
    public string? FromVillagePatientErrorMessage => GetPropertyErrorMessage(nameof(FromVillagePatient));
    public string? MothersWithPatientsErrorMessage => GetPropertyErrorMessage(nameof(MothersWithPatients));
    public string? MothersDischargedErrorMessage => GetPropertyErrorMessage(nameof(MothersDischarged));

    #endregion

    #region Helper Methods

    private void NotifyDependentProperties(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
            OnPropertyChanged(propertyName);
    }

    private bool IsBedsAvailable(out int availableBeds)
    {
        availableBeds = KoikiCount ?? 0;
        return KoikiCount.HasValue && KoikiCount.Value > 0;
    }

    private string ValidatePartExceedsWhole(int? part, int? whole, string errorMessage)
    {
        if (part.HasValue && whole.HasValue && part > whole)
            return errorMessage;
        return null;
    }

    #endregion

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    public void ValidateAll()
    {
        ValidateMany(
            nameof(PatientsAtStartOfDay),
            nameof(TotalAdmitted),
            nameof(FromVillage),
            nameof(ChildrenUnder14),
            nameof(TransferredFromOtherDepartments),
            nameof(TransferredToOtherDepartments),
            nameof(TotalDischarged),
            nameof(DischargedByTransfer),
            nameof(Died),
            nameof(TotalPatients),
            nameof(FromVillagePatient),
            nameof(MothersWithPatients),
            nameof(MothersDischarged));
    }

    private void ValidateMany(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames.Distinct())
            ValidateOne(propertyName);
    }

    private void ValidateOne(string propertyName)
    {
        string? err = propertyName switch
        {
            nameof(TotalPatients) => ValidateTotalPatients(),
            nameof(PatientsAtStartOfDay) => ValidatePatientsAtStartOfDay(),
            nameof(TotalAdmitted) => ValidateTotalAdmitted(),
            nameof(FromVillage) => ValidateFromVillage(),
            nameof(ChildrenUnder14) => ValidateChildrenUnder14(),
            nameof(TransferredFromOtherDepartments) => ValidateTransferredFromOtherDepartments(),
            nameof(TotalDischarged) => ValidateTotalDischarged(),
            nameof(TransferredToOtherDepartments) => ValidateTransferredToOtherDepartments(),
            nameof(DischargedByTransfer) => ValidateDischargedByTransfer(),
            nameof(Died) => ValidateDied(),
            nameof(FromVillagePatient) => ValidateFromVillagePatient(),
            nameof(MothersWithPatients) => ValidateMothersWithPatients(),
            nameof(MothersDischarged) => ValidateMothersDischarged(),
            _ => null
        };

        SetFieldError(propertyName, err);
    }

    private string ValidateTotalPatients()
    {
        if (IsBedsAvailable(out int availableBeds) && TotalPatients > availableBeds)
            return string.Format(ValidationMessages.PatientsExceedBeds, TotalPatients, availableBeds);
        return null;
    }

    private string ValidatePatientsAtStartOfDay()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(PatientsAtStartOfDay?.ToString(), "Состояло", out _, isOptional: false));

        if (string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(CrossRowError))
            return CrossRowError;

        if (string.IsNullOrEmpty(result) && IsBedsAvailable(out int availableBeds))
        {
            if (PatientsAtStartOfDay.HasValue && PatientsAtStartOfDay.Value > availableBeds)
                return string.Format(ValidationMessages.PatientsAtStartExceedsBeds, PatientsAtStartOfDay.Value, availableBeds);
        }

        if (string.IsNullOrEmpty(result) && PatientsAtStartOfDay.HasValue && TotalDischarged.HasValue)
        {
            if (PatientsAtStartOfDay.Value < TotalDischarged.Value)
                return string.Format(ValidationMessages.PatientsAtStartLessThanDischarged, PatientsAtStartOfDay.Value, TotalDischarged.Value);
        }

        if (string.IsNullOrEmpty(result) && IsBedsAvailable(out int beds))
        {
            if (PatientsAtStartOfDay.HasValue && TotalAdmitted.HasValue)
            {
                int sum = PatientsAtStartOfDay.Value + TotalAdmitted.Value;
                if (sum > beds)
                    return string.Format(ValidationMessages.PatientsAtStartPlusAdmittedExceedsBeds,
                        PatientsAtStartOfDay.Value, TotalAdmitted.Value, beds);
            }
        }

        return result;
    }

    private string ValidateTotalAdmitted()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(TotalAdmitted?.ToString(), "Поступило Всего", out _, isOptional: false));

        if (string.IsNullOrEmpty(result))
            result = ValidatePartExceedsWhole(FromVillage, TotalAdmitted, ValidationMessages.FromVillageExceedsTotalAdmitted);

        if (string.IsNullOrEmpty(result))
            result = ValidatePartExceedsWhole(ChildrenUnder14, TotalAdmitted, ValidationMessages.ChildrenExceedsTotalAdmitted);

        if (string.IsNullOrEmpty(result) && IsBedsAvailable(out int beds))
        {
            if (PatientsAtStartOfDay.HasValue && TotalAdmitted.HasValue)
            {
                int sum = PatientsAtStartOfDay.Value + TotalAdmitted.Value;
                if (sum > beds)
                    return string.Format(ValidationMessages.PatientsAtStartPlusAdmittedExceedsBeds,
                        PatientsAtStartOfDay.Value, TotalAdmitted.Value, beds);
            }
        }

        return result;
    }

    private string ValidateFromVillage()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(FromVillage?.ToString(), "Поступило Сельских", out _, isOptional: true));
        if (string.IsNullOrEmpty(result))
            result = ValidatePartExceedsWhole(FromVillage, TotalAdmitted, ValidationMessages.FromVillageExceedsTotalAdmitted);
        return result;
    }

    private string ValidateChildrenUnder14()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(ChildrenUnder14?.ToString(), "Поступило Детей", out _, isOptional: true));
        if (string.IsNullOrEmpty(result))
            result = ValidatePartExceedsWhole(ChildrenUnder14, TotalAdmitted, ValidationMessages.ChildrenExceedsTotalAdmitted);
        return result;
    }

    private string ValidateTransferredFromOtherDepartments()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(TransferredFromOtherDepartments?.ToString(), "Переведено Из других", out _, isOptional: true));
        if (string.IsNullOrEmpty(result) && TransferredFromOtherDepartments.HasValue)
        {
            if (TransferredFromOtherDepartments.Value > TotalPatients)
                return string.Format(ValidationMessages.TransferredFromExceedsTotalPatients,
                    TransferredFromOtherDepartments.Value, TotalPatients);
        }
        return result;
    }

    private string ValidateTotalDischarged()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(TotalDischarged?.ToString(), "Выписано Всего", out _, isOptional: false));
        if (string.IsNullOrEmpty(result))
            result = ValidatePartExceedsWhole(DischargedByTransfer, TotalDischarged, ValidationMessages.DischargedByTransferExceedsTotal);
        if (string.IsNullOrEmpty(result))
            result = ValidatePartExceedsWhole(MothersDischarged, TotalDischarged, ValidationMessages.MothersDischargedExceedsTotal);
        if (string.IsNullOrEmpty(result) && PatientsAtStartOfDay.HasValue && TotalDischarged.HasValue)
        {
            if (PatientsAtStartOfDay.Value < TotalDischarged.Value)
                return string.Format(ValidationMessages.PatientsAtStartLessThanDischarged, PatientsAtStartOfDay.Value, TotalDischarged.Value);
        }
        return result;
    }

    private string ValidateTransferredToOtherDepartments()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(TransferredToOtherDepartments?.ToString(), "Переведено В другие", out _, isOptional: true));
        if (string.IsNullOrEmpty(result) && TransferredToOtherDepartments.HasValue)
        {
            if (TransferredToOtherDepartments.Value > TotalPatients)
                return string.Format(ValidationMessages.TransferredToExceedsTotalPatients,
                    TransferredToOtherDepartments.Value, TotalPatients);
        }
        return result;
    }

    private string ValidateDischargedByTransfer()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(DischargedByTransfer?.ToString(), "Выписано в другие стационары", out _, isOptional: true));
        if (string.IsNullOrEmpty(result))
            result = ValidatePartExceedsWhole(DischargedByTransfer, TotalDischarged, ValidationMessages.DischargedByTransferExceedsTotal);
        return result;
    }

    private string ValidateDied()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(Died?.ToString(), "Умерло", out _, isOptional: false));
        if (string.IsNullOrEmpty(result) && Died.HasValue && PatientsAtStartOfDay.HasValue)
        {
            if (Died.Value > PatientsAtStartOfDay.Value)
                return string.Format(ValidationMessages.DiedExceedsPatientsAtStart, Died.Value, PatientsAtStartOfDay.Value);
        }
        if (string.IsNullOrEmpty(result) && Died.HasValue)
        {
            int patientsCountable = (PatientsAtStartOfDay ?? 0) + (TotalAdmitted ?? 0) + (TransferredFromOtherDepartments ?? 0);
            if (Died.Value > patientsCountable)
                return string.Format(ValidationMessages.DiedExceedsPatients, patientsCountable);
        }
        return result;
    }

    private string ValidateFromVillagePatient()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(FromVillagePatient?.ToString(), "Состоит Сельских", out _, isOptional: true));
        if (string.IsNullOrEmpty(result))
            result = ValidatePartExceedsWhole(FromVillagePatient, TotalPatients, ValidationMessages.FromVillagePatientExceedsTotal);
        if (string.IsNullOrEmpty(result) && IsBedsAvailable(out int availableBeds))
        {
            if (FromVillagePatient.HasValue && FromVillagePatient.Value > availableBeds)
                return string.Format(ValidationMessages.FromVillagePatientExceedsBeds, availableBeds);
        }
        return result;
    }

    private string ValidateMothersWithPatients()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(MothersWithPatients?.ToString(), "Состоит матерей", out _, isOptional: true));
        if (string.IsNullOrEmpty(result))
            result = ValidatePartExceedsWhole(MothersWithPatients, TotalPatients, ValidationMessages.MothersWithPatientsExceedsTotal);
        if (string.IsNullOrEmpty(result) && MothersWithPatients.HasValue)
        {
            int totalChildren = TotalPatients;
            if (MothersWithPatients.Value > totalChildren)
                return string.Format(ValidationMessages.MothersExceedsChildren, MothersWithPatients.Value, totalChildren);
        }
        if (string.IsNullOrEmpty(result) && IsBedsAvailable(out int availableBeds))
        {
            if (MothersWithPatients.HasValue && MothersWithPatients.Value > availableBeds)
                return string.Format(ValidationMessages.MothersWithPatientsExceedsBeds, availableBeds);
        }
        return result;
    }

    private string ValidateMothersDischarged()
    {
        string? result = NullIfEmpty(ValidationHelper.ValidateIntInput(MothersDischarged?.ToString(), "Выписано матерей", out _, isOptional: true));
        if (string.IsNullOrEmpty(result))
            result = ValidatePartExceedsWhole(MothersDischarged, TotalDischarged, ValidationMessages.MothersDischargedExceedsTotal);
        if (string.IsNullOrEmpty(result) && MothersDischarged.HasValue && MothersWithPatients.HasValue)
        {
            if (MothersDischarged.Value > MothersWithPatients.Value)
                return string.Format(ValidationMessages.MothersDischargedExceedsMothersWithPatients,
                    MothersDischarged.Value, MothersWithPatients.Value);
        }
        return result;
    }

}

