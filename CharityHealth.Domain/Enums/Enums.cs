namespace CharityHealth.Domain.Enums;

public enum UserType
{
    Beneficiary = 1,
    Doctor = 2,
    Staff = 3,
    Administrator = 4
}

public enum RequestStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    Approved = 4,
    Rejected = 5,
    Completed = 6
}

public enum LoginMethod
{
    UsernamePassword = 1,
    MobileOtp = 2
}

public enum DocumentType
{
    NationalId = 1,
    MedicalReport = 2,
    IncomeProof = 3,
    Other = 4
}

public enum NotificationType
{
    RequestApproved = 1,
    RequestRejected = 2,
    DocumentRequired = 3,
    AppointmentReminder = 4,
    ConsultationCompleted = 5
}

public enum Gender
{
    Male = 1,
    Female = 2
}
