namespace SplitWise.Domain;

public class SplitWiseConstants
{
    #region Exceptions
    public const string INTERNAL_SERVER = "An error occurred while processing the request";
    public const string RECORD_NOT_FOUND = "Record Not Found!";
    public const string RECORD_ALREADY_EXISTS = "Record Already Exists!";
    public const string INVALID_LOGIN = "Invalid login credentials!";
    public const string REFRESH_TOKEN_EXPIRED = "Refresh token was expired.";
    public const string INVALID_RESET_PASS_TOKEN = "Invalid reset password token or token was expired!";
    public const string INVALID_FILE_UPLOAD = "Invalid file upload.";
    public const string OLD_PASSWORD_WRONG = "Current password is incorrect";
    public const string UN_AUTHORIZED = "User is unauthorized.";
    public const string ACCESS_DENIED = "Access denied.";
    public const string SUPER_ADMIN_ADD = "SuperAdmin role must exist before seeding users.";
    public const string UserDisabled = "Your account has been disabled by the administrator. Please contact support for assistance.";
    public const string ENDDATE_MUST_GREATER = "End date must be greater than or equal to Start date.";
    public const string SAME_LEAVETYPE = "For a single-day leave, StartLeaveType and EndLeaveType must always be the same.";
    public const string SELECTION_OF_LEAVETYPE = "For a single-day leave, the leave type must be either FullDay, FirstHalf, or SecondHalf.";
    public const string FULLDAY_LEAVETYPE = "Please Select a valid Leave Type.";

    #endregion

    #region Messages
    public const string SUCCESS_MESSAGE = "Success";
    public const string RECORD_CREATED = "Record Created Successfully!";
    public const string RECORD_UPDATED = "Record Updated Successfully!";
    public const string RECORD_DELETED = "Record Deleted Successfully!";
    public const string REGISTER = "User registration successfully!";
    public const string FORGOT_PASSWORD = "Reset password link sent to your email!";
    public const string SEND_REMINDER = "Permit expiry reminder has been sent to registered email!";
    public const string CHANGE_PASSWORD = "Your password has been changed successfully!";
    public const string LOGOUT = "Logout successfully!";
    public const string REQUEST_SUCCESS = "Request successfully!";
    public const string APPROVED_REJECTED_SUCCESS = "Application {0} successfully!";
    public const string CUSTOMER_PROFILE_UPDATE_MESSAGE = "profile updated successfully.";
    public const string ACCEPT = "Accepted";
    public const string REJECT = "Rejected";
    public const string ACCEPT_PERMIT = "Accepted Permit";
    public const string REJECT_PERMIT = "Rejected Permit";
    #endregion

    #region Validations
    public const string REQUIRED = "{0} is required.";
    public const string MAX_CHAR = "{0} must not exceed {1} characters.";
    public const string INVALID = "Invalid {0}.";
    public const string BETWEEN_CHAR = "{0} must be between {1} and {2} characters.";
    public const string MATCH_CHAR = "{0} and {1} must match.";
    public const string EMAIL_REGEX = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    public const string STR_CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$%&*";
    #endregion

    #region System
    public const int PAGE_SIZE = 10;
    public const string AVATAR_PATH = "avatars";
    public const string APPROVED = "Approved";
    public const string DATE_FORMAT = "dd MMM yyyy";
    public const int KEY_SIZE = 64;
    public const int ITERATION_COUNT = 1000000;
    public const string PASSWORD_KEY = "PasswordKey";
    public const string RESET_PASS_EXP = "ResetPasswordLinkExpiryMinutes";
    public const string ENCRYPTION_KEY = "EncryptionKey";
    public const string FE_URL = "FrontEndUrl";
    public const string USER_ID = "UserId";
    public const string EMAIL = "Email";
    public const string ROLE_ID = "RoleId";
    public const string ROLE = "Role";
    public const string NAME = "Name";
    public const string JWT_SETTINGS = "JwtSettings";
    public const string SECURITY_SETTINGS = "SecuritySettings";
    public const string EMAIL_SETTINGS = "EmailSettings";
    public const string SUPER_ADMIN_SETTINGS = "SuperAdminCredential";
    public const string HAS_PERMISSION = "HasPermission";
    public const string ANGULAR_CORS = "AngularOrigin";
    public const string IMAGE_SPECIFICATION = "data:image/png;base64";
    #endregion

    #region Email
    public const string RESET_PASS_EMAIl_SUBJECT = "Reset Password Link | IAS system";
    public const string PERMIT_REMINDER_EMAIl_SUBJECT = "Permit Renewal Reminder | IAS system";
    public const string PERMIT_INSPECTION_FINALIZED = "Permit Inspection Scheduled | IAS system";
    public const string TEMPLATES_FOLDER = "Templates";
    public const string RESET_PASS_EMAIL_TEMPLATE = "ResetPasswordEmailTemplate.html";
    public const string PERMIT_REMINDER_EMAIL_TEMPLATE = "PermitReminderEmailTemplate.html";
    public const string PROFILE_APPROVE_REJECT_SUBJECT = "Your profile has been {0} | IAS system";
    public const string INVITATION = "Invitation | SpliwtWise";
    public const string PROFILE_APPROVE_REJECT_EMAIL_TEMPLATE = "ProfileApproveRejectEmailTemplate.html";
    public const string INVITE_FRIEND = "InviteFriend.html";
    public const string SYS_GEN_PASS_EMAIL_TEMPLATE = "SystemGeneratedPassword.html";
    public const string PERMIT_INSPECTION_FINALIZED_TEMPLATE = "PermitInspectionFinalizedTemplate.html";

    #endregion

    #region PATH
    public const string STATIC_FILE_FOLDER = "wwwroot";
    public const string CUSTOMER_FOLDER = "customer";
    public const string DOCUMENT_FOLDER = "document";

    #endregion
}
