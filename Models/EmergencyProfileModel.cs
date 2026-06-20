using System.Collections.ObjectModel;

namespace NextBand.Models;

public sealed class EmergencyProfileModel : ObservableModel
{
    private string _childName = string.Empty;
    private string _age = string.Empty;
    private string _bloodType = string.Empty;
    private string _guardians = string.Empty;
    private string _mainPhone = string.Empty;
    private string _address = string.Empty;
    private string _allergies = string.Empty;
    private string _medicalConditions = string.Empty;
    private string _disabilities = string.Empty;
    private string _specialNeeds = string.Empty;
    private string _medications = string.Empty;
    private string _emergencyInstructions = string.Empty;

    public string ChildName { get => _childName; set => SetProperty(ref _childName, value); }
    public string Age { get => _age; set => SetProperty(ref _age, value); }
    public string BloodType { get => _bloodType; set => SetProperty(ref _bloodType, value); }
    public string Guardians { get => _guardians; set => SetProperty(ref _guardians, value); }
    public string MainPhone { get => _mainPhone; set => SetProperty(ref _mainPhone, value); }
    public string Address { get => _address; set => SetProperty(ref _address, value); }
    public string Allergies { get => _allergies; set => SetProperty(ref _allergies, value); }
    public string MedicalConditions { get => _medicalConditions; set => SetProperty(ref _medicalConditions, value); }
    public string Disabilities { get => _disabilities; set => SetProperty(ref _disabilities, value); }
    public string SpecialNeeds { get => _specialNeeds; set => SetProperty(ref _specialNeeds, value); }
    public string Medications { get => _medications; set => SetProperty(ref _medications, value); }
    public string EmergencyInstructions { get => _emergencyInstructions; set => SetProperty(ref _emergencyInstructions, value); }
    public ObservableCollection<EmergencyContactModel> ExtraContacts { get; set; } = [];
    public ObservableCollection<CustomInfoModel> CustomInfos { get; set; } = [];
}
