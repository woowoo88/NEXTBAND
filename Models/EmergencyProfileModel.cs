using System.Collections.ObjectModel;

namespace NextBand.Models;

public sealed class EmergencyProfileModel : ObservableModel
{
    private string _childName = "Maria Silva";
    private string _age = "7";
    private string _bloodType = "O+";
    private string _guardians = "Ana Silva e Joao Santos";
    private string _mainPhone = "(11) 123456789";
    private string _address = "ali mesmo que vc ta pensando";
    private string _allergies = "Amendoim";
    private string _medicalConditions = "Asma";
    private string _disabilities = "Sindrome de Down";
    private string _specialNeeds = "Atencao Especial";
    private string _medications = string.Empty;
    private string _emergencyInstructions = "Em caso de crise, usar inalador. Levar ao hospital mais proximo.";

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
