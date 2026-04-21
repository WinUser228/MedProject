using HospitalStatistics.Core.Entities;
using MedProject.Utilities;
using System;

namespace MedProject.Models;

public abstract class BedBaseViewModel : ViewModelValidationBase
{
    protected bool _shouldValidate;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private BedTypes _selectedBedType;
    public BedTypes SelectedBedType
    {
        get => _selectedBedType;
        set
        {
            if (SetProperty(ref _selectedBedType, value) && _shouldValidate)
                ValidateProperty(nameof(SelectedBedType));
        }
    }

    public string BedType => SelectedBedType?.BedTypeName;

    private Departments _selectedDepartment;
    public Departments SelectedDepartment
    {
        get => _selectedDepartment;
        set
        {
            if (SetProperty(ref _selectedDepartment, value))
            {
                DepartmentId = value?.Id ?? 0;
                if (_shouldValidate)
                    ValidateProperty(nameof(SelectedDepartment));
            }
        }
    }

    private int _departmentId;
    public int DepartmentId
    {
        get => _departmentId;
        set => SetProperty(ref _departmentId, value);
    }

    private string _amountInput = string.Empty;
    public string AmountInput
    {
        get => _amountInput ?? string.Empty;
        set
        {
            if (SetProperty(ref _amountInput, value ?? string.Empty) && _shouldValidate)
                ValidateProperty(nameof(AmountInput));
        }
    }

    private int _amount;
    public int Amount
    {
        get => _amount;
        protected set => SetProperty(ref _amount, value);
    }

    private DateTime? _effectiveDate;
    public DateTime? EffectiveDate
    {
        get => _effectiveDate;
        set
        {
            if (SetProperty(ref _effectiveDate, value))
            {
                if (_shouldValidate)
                {
                    ValidateProperty(nameof(EffectiveDate));
                    if (NotUsedSince.HasValue)
                        ValidateProperty(nameof(NotUsedSince));
                }
            }
        }
    }

    private DateTime? _notUsedSince;
    public DateTime? NotUsedSince
    {
        get => _notUsedSince;
        set
        {
            if (SetProperty(ref _notUsedSince, value))
            {
                if (_shouldValidate)
                {
                    ValidateProperty(nameof(NotUsedSince));
                    if (EffectiveDate.HasValue)
                        ValidateProperty(nameof(EffectiveDate));
                }
            }
        }
    }

    private string _monthlyPlanInput = string.Empty;
    public string MonthlyPlanInput
    {
        get => _monthlyPlanInput ?? string.Empty;
        set
        {
            if (SetProperty(ref _monthlyPlanInput, value ?? string.Empty) && _shouldValidate)
                ValidateProperty(nameof(MonthlyPlanInput));
        }
    }

    private int _monthlyPlan;
    public int MonthlyPlan
    {
        get => _monthlyPlan;
        protected set => SetProperty(ref _monthlyPlan, value);
    }

    protected void ValidateProperty(string propertyName)
    {
        if (IsLoading)
        {
            SetFieldError(propertyName, null);
            return;
        }

        if (!_shouldValidate)
        {
            SetFieldError(propertyName, null);
            return;
        }

        string? error = null;

        switch (propertyName)
        {
            case nameof(SelectedBedType):
                error = SelectedBedType == null ? "Необходимо выбрать тип койки." : null;
                break;
            case nameof(SelectedDepartment):
                error = SelectedDepartment == null ? "Необходимо выбрать отделение." : null;
                break;
            case nameof(DepartmentId):
                error = ValidationHelper.ValidateId(DepartmentId, "Отделение");
                break;
            case nameof(AmountInput):
                error = ValidationHelper.ValidateIntInput(AmountInput, "Количество", out int parsedAmount);
                if (string.IsNullOrEmpty(error))
                {
                    Amount = parsedAmount;
                    if (Amount < 0)
                        error = "Количество не может быть отрицательным.";
                }
                break;
            case nameof(EffectiveDate):
                if (!EffectiveDate.HasValue)
                    error = "Необходимо указать дату начала действия.";
                else if (NotUsedSince.HasValue && EffectiveDate.Value.Date >= NotUsedSince.Value.Date)
                    error = "Дата начала действия должна быть раньше даты, когда койка перестала использоваться.";
                break;
            case nameof(NotUsedSince):
                if (EffectiveDate.HasValue && NotUsedSince.HasValue && EffectiveDate.Value.Date >= NotUsedSince.Value.Date)
                    error = "Дата прекращения использования должна быть позже даты начала действия.";
                break;
            case nameof(MonthlyPlanInput):
                error = ValidationHelper.ValidateIntInput(MonthlyPlanInput, "Месячный план", out int parsedMonthlyPlan);
                if (string.IsNullOrEmpty(error))
                {
                    MonthlyPlan = parsedMonthlyPlan;
                    if (MonthlyPlan < 0)
                        error = "Месячный план не может быть отрицательным.";
                }
                break;
        }

        SetFieldError(propertyName, error);
    }

    protected void ValidateAllProperties()
    {
        ValidateProperty(nameof(SelectedBedType));
        ValidateProperty(nameof(SelectedDepartment));
        ValidateProperty(nameof(DepartmentId));
        ValidateProperty(nameof(AmountInput));
        ValidateProperty(nameof(EffectiveDate));
        ValidateProperty(nameof(NotUsedSince));
        ValidateProperty(nameof(MonthlyPlanInput));
    }

    protected bool HasValidationErrors()
    {
        _shouldValidate = true;
        ValidateAllProperties();
        return HasErrors;
    }

    protected void TriggerValidationUIUpdate()
    {
        _shouldValidate = true;
        OnPropertyChanged(nameof(SelectedBedType));
        OnPropertyChanged(nameof(SelectedDepartment));
        OnPropertyChanged(nameof(AmountInput));
        OnPropertyChanged(nameof(EffectiveDate));
        OnPropertyChanged(nameof(NotUsedSince));
        OnPropertyChanged(nameof(MonthlyPlanInput));
        ValidateAllProperties();
    }

    protected abstract void RaiseCanExecuteChangedForCommands();
}
