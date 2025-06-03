export class GlobalConstant {
  // String constant...
  public static readonly PATH_MATCH = 'full';
  public static readonly PATTERN = 'pattern';
  public static readonly REQUIRED = 'required';
  public static readonly MIN_LENGTH = 'minlength';
  public static readonly MAX_LENGTH = 'maxlength';
  public static readonly MIN = 'min';
  public static readonly MISMATCH = 'mismatch';
  public static readonly CHART_ORDER = 'chartOrder';
  public static readonly LENGTH = 'mismatch';

  // Validation message constant...
  public static readonly EMAIL_REGEX =
    /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
  public static readonly INVALID_EMAIL = 'Invalid Email!';
  public static readonly EMAIL_IS_REQUIRED = 'Email is required!';
  public static readonly PASSWORD_IS_REQUIRED = 'Password is required!';
  public static readonly PASSWORD_MIN_LENGTH =
    'Password contains min 6 character';
  public static readonly PASSWORD_MAX_LENGTH =
    'Password contains max 12 character';
  public static readonly PASSWORD_MATCH =
    'Password and Confirm Password must match';
  public static readonly PHONE_NO_REGEX = /^[0-9]{10}$/;
  public static readonly FIRST_NAME_IS_REQUIRED = 'First Name is required';
  public static readonly LAST_NAME_IS_REQUIRED = 'Last Name is required';
  public static readonly PHONE_IS_REQUIRED = 'Phone no is required';
  public static readonly INVALID_VALID_NO = 'Invalid phone no';
  public static readonly MOBILE_NUMBER_PATTERN = '^[0-9]{10}$';
  public static readonly NAME_IS_REQUIRED = 'Name is required';
  public static readonly MESSAGE_IS_REQUIRED = 'Message is required';

  static readonly IS_REQUIRED = 'is required';
  static readonly MIN_POSITIVE = 'must be greater than 0';
  static readonly AD_SERVICE_TYPE_REQUIRED =
    'Advertisement Service Type is required';
  static readonly TITLE_REQUIRED = 'Title is required';
  static readonly TITLE_MAX = 'Title cannot exceed 100 characters';
  static readonly TAGLINE_REQUIRED = 'Tagline is required';
  static readonly TAGLINE_MAX = 'Tagline cannot exceed 200 characters';
  static readonly DESCRIPTION_REQUIRED = 'Description is required';
  static readonly DESCRIPTION_MAX = 'Description cannot exceed 200 characters';
  static readonly IMAGE_REQUIRED = 'Image is required';
  static readonly LOCATION_REQUIRED = 'Location is required';
  static readonly LENGTH_REQUIRED = 'Length is required';
  static readonly LENGTH_MIN = 'Length must be greater than 0';
  static readonly HEIGHT_REQUIRED = 'Height is required';
  static readonly HEIGHT_MIN = 'Height must be greater than 0';
  static readonly SIZE_REQUIRED = 'Size is required';
  static readonly PRICE_REQUIRED = 'Price is required';
  static readonly PRICE_MIN = 'Price must be greater than 0';
  static readonly PRICE_TITLE_REQUIRED = 'Price Title is required';
  static readonly PRICE_TITLE_MAX = 'Price Title cannot exceed 100 characters';
  static readonly PRICE_DESCRIPTION_REQUIRED = 'Price Description is required';
  static readonly PRICE_DESCRIPTION_MAX =
    'Price Description cannot exceed 200 characters';
  static readonly FEATURE_TITLE_REQUIRED = 'Feature title is required';
  static readonly FEATURE_TITLE_MAX =
    'Feature title cannot exceed 100 characters';
  static readonly FEATURE_DESCRIPTION_REQUIRED =
    'Feature description is required';
  static readonly FEATURE_DESCRIPTION_MAX =
    'Feature description cannot exceed 200 characters';
  static readonly BENEFIT_TITLE_REQUIRED = 'Benefit title is required';
  static readonly BENEFIT_TITLE_MAX =
    'Benefit title cannot exceed 100 characters';
  static readonly BENEFIT_DESCRIPTION_REQUIRED =
    'Benefit description is required';
  static readonly BENEFIT_DESCRIPTION_MAX =
    'Benefit description cannot exceed 200 characters';
  public static readonly PER_FREQUENCY_PRICE_IS_REQUIRED =
    'Per frequency price is required';
  public static readonly PER_DURATION_PRICE_IS_REQUIRED =
    'Per duration price is required';
  public static readonly ADBOARD_NAME_IS_REQUIRED = 'Ad board name is required';
  public static readonly DESCRIPTION_IS_REQUIRED = 'description is required';
  public static readonly LATITUDE_IS_REQUIRED = 'Latitude is required';
  public static readonly LONGITUDE_IS_REQUIRED = 'Longitude is required';
  public static readonly FREQUENCY_IS_REQUIRED = 'Frequency is required';
  public static readonly DURATION_IS_REQUIRED = 'Duration is required';
  public static readonly OTHER_TYPE_IS_REQUIRED = 'Other type is required';
  public static readonly CLEAR_ALL_NOTIFICATION = 'Clear All Notifications.';
  public static readonly CONFIRM_DELETE =
    'Are you sure you want to delete this message?';

  // Application constant...
  public static readonly ACCESS_TOKEN = 'access_token';
  public static readonly REFRESH_TOKEN = 'refresh_token';
  public static readonly SESSION_EXPIRED =
    'Session expired. Please log in again.';
  public static readonly LOGIN_SUCCESS: 'Login successful. Welcome back!';
  public static readonly ERROR_MESSAGE: 'Something went wrong. Please try again later.';
  public static readonly PAGE_SIZE: 10;
  public static readonly DEFAULT_DIALOG_WIDTH = 400;
  public static readonly DEFAULT_DEBOUNCE_TIME = 500;
  public static readonly DEFAULT_AVATAR = 'images/avatar.jpg';
  public static readonly CUSTOMER_CARD_FILE_NAME = 'customer-card';
  public static readonly IMAGE_AND_VIDEO_FILE_TYPE = [
    'jpg',
    'jpeg',
    'png',
    'mp4',
    'mov',
    'avi',
    '.wmv',
    '.webm',
  ];
  public static readonly DEFAULT_TRUNCATE_CHARACTER = 185;

  // App Routes constant...
  public static readonly DASHBOARD = 'dashboard';
  public static readonly LEAVE = 'leave';
  public static readonly ADMIN = 'admin';
  public static readonly SUPERADMIN = 'superadmin';
  public static readonly STAFFMEMBER = 'staffmember';
  public static readonly INSPECTOR = 'inspector';
  public static readonly CUSTOMER = 'customer';
  public static readonly AUTH = 'auth';
  public static readonly LOGIN = 'login';
  public static readonly REGISTER = 'register';
  public static readonly FORGOT = 'forgot';
  public static readonly RESET_PASSWORD = 'reset-password';
  public static readonly ROLE = 'role';
  public static readonly ROLE_PERMISSION = 'role-permission';
  public static readonly ROLE_PERMISSION_PARAMS = '/:id/:name';
  public static readonly ACCESS_DENIED = 'access-denied';
  public static readonly PROFILE = 'profile';
  public static readonly CUSTOMER_PROFILE = 'customer-profile';
  public static readonly ADVERTISER_CARD = 'advertiser-card';
  public static readonly PERMIT_ADD = 'permit';
  public static readonly PERMIT_EDIT = 'permit/:id';
  public static readonly ALLPERMITDETAIL = 'all-permit-detail/:id/:slotId';
  public static readonly ALLPERMITDETAILWITHOUTSLOTDETAIL =
    'all-permit-detail/:id';
  public static readonly PERMIT_CARD = 'permit-card';
  public static readonly PERMIT_PAYMENT = 'permit-payment';
  public static readonly PERMIT_COMPLAIN = 'permit/complain/:id';
  public static readonly CUSTOMER_ROLE = 'Customer';
  public static readonly SUPER_ADMIN_ROLE = 'SuperAdmin';
  public static readonly STAFF_MEMBER_ROLE = 'StaffMember';
  public static readonly INSPECTOR_ROLE = 'Inspector';
  public static readonly ADMIN_ROLE = 'Admin';
  // Customer constant...
  public static readonly DIRECT_ADVERTISER = 'DirectAdvertiser';
  public static readonly BUSINESS_FILE = 'businessCertificate';
  public static readonly TAX_FILE = 'taxCertificate';

  // Messages
  public static readonly ONLY_PDF_ALLOWED = 'Only pdf file allowed';
  public static readonly IMAGE_ALLOWED =
    'Invalid file type. Only jpg, jpeg or png are allowed.';
  public static readonly CUSTOMER_APPROVAL = 'customer-approval';
  public static readonly CUSTOMER_LIST = 'customer-list';
  public static readonly USER_LIST = 'user-list';
  public static readonly COUNTRY_LIST = 'country-list';
  public static readonly STATE_LIST = 'state-list';
  public static readonly CITY_LIST = 'city-list';
  public static readonly ADVERTISER_CONFIGURATION = 'advertiser-configuration';
  public static readonly PERMIT_LIST = 'permit-list';
  public static readonly PERMIT_ASSIGNMENT = 'permit-assignment';
  public static readonly ADVERTISING_AREA = 'advertising-area-list';
  public static readonly ADVERTISING_AREA_INSPECTOR =
    'advertising-area-inspector-list';
  public static readonly ADVERTISING_SERVICE_DETAILS_MASTER =
    'advertising-service-details-master';
  public static readonly ADVERTISING_SERVICE_DETAILS_MASTER_ADD =
    'advertisement-detail-master/add';
  public static readonly ADVERTISING_SERVICE_DETAILS_MASTER_EDIT =
    'advertisement-detail-master/add/:id';
  public static readonly ADVERTISING_SERVICE_TYPE =
    'advertisement-service-type';
  public static readonly AREA_LIST = 'area-list';
  public static readonly REJECTION_REASON_ALERT =
    '⚠️ Please enter a rejection reason.';
  public static readonly ACCEPTED = 'Accepted';
  public static readonly APPROVED = 'Approved';
  public static readonly INSPECTOR_ASSIGN_SUCCESS_MESSAGE =
    'Inspector Assigned Successfully!';
  public static readonly INSPECTOR_ASSIGN_ERROR_MESSAGE =
    'Failed to Assign Inspector.';
  public static readonly CUSTOMER_REPORT = 'customer-report';
  public static readonly NO_DATA_FOUND = 'No data available to export.';
  public static readonly SALE_REPORT = 'sale-report';
  public static readonly REVENUE_REPORT = 'revenue-report';
  public static readonly MAINTENANCE = 'maintenance';
  public static readonly CUSTOMER_COMPLAIN = 'customer-complain';
  public static readonly DISPLAY_SLOTS_DETAIL = 'display-slots-detail';
  public static readonly STAFF_COMPLAIN = 'complain';
  public static readonly COMPLAIN_REPORT = 'complain-report';
  public static readonly MAINTENANCE_REPORT = 'maintenance-report';
  public static readonly REQUIRED_CITY = 'City is required Field';
  public static readonly REQUIRED_CUSTOMERTYPE =
    'Customer type is required Field';
  public static readonly ACCOUNT_STATUS = 'accountStatus';
  public static readonly CUSTOMER_TYPE = 'customerType';
  public static readonly MAINTENANCE_STATUS = 'maintenanceStatus';
  public static readonly APPLICATION_STATUS = 'applicationStatus';
  public static readonly BILLBOARD_TYPE = 'billBoardType';
  public static readonly STREET_ADDRESS = 'Street address is required Field';
  public static readonly ZIP_CODE = 'Zip code is required Field';
  public static readonly ADDRESS = 'Please select address in the Map.';

  //image
  public static readonly IMAGE =
    'https://hebbkx1anhila5yf.public.blob.vercel-storage.com/image-OZvRP3tqSRSJlW642WWf8CmtsyKcTN.png';

  //error
  public static readonly ENDDATE_MUST_GREATER =
    'End Date must be greater than or equal to Start Date.';

  public static readonly SAME_LEAVETYPE =
    'Start Leave Type and End Leave Type must be the same.';

  //Inspector constant...
  public static readonly ASSIGNED = 'Assigned';

  // Map constant
  public static readonly MAP_DEFAULT_CITY = 'Ahmedabad';
  public static readonly MAP_DEFAULT_TILE_LAYER =
    'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
  public static readonly MAP_GEOLOCATION_API =
    'https://nominatim.openstreetmap.org/search?format=json&q=';
  public static readonly MAP_PIN_LABEL_AVAILABLE = 'Available';
  public static readonly MAP_PIN_LABEL_SELECTED = 'Selected';
  public static readonly MAP_PIN_USER_SELECTED = 'UserSelected';
  public static readonly MAP_GREEN_PIN = 'images/marker-green.png';
  public static readonly MAP_GREEN_RED = 'images/marker-red.png';
  public static readonly MAP_RED_PIN = 'images/marker-red.png';
  public static readonly MAP_BLUE_PIN = 'images/marker-blue.png';

  //File Name Constans..
  public static readonly CUSTOMER_REPORT_FILE_NAME = 'customer-report-data.csv';
  public static readonly SALE_REPORT_FILE_NAME = 'sale-report-data.csv';
  public static readonly REVENUE_REPORT_FILE_NAME = 'revenue-chart.pdf';
  public static readonly COMPLAIN_REPORT_FILE_NAME = 'complain_report.csv';
  public static readonly MAINTENANCE_REPORT_FILE_NAME =
    'maintenance-report.csv';
}

// Other Constants...
export const PAGE_OPTIONS = [10, 20, 50];

export const EVENT_TYPES = {
  ADD: 'add',
  EDIT: 'edit',
  DELETE: 'delete',
  VIEW: 'view',
  APPROVE: 'approve',
  REJECT: 'reject',
  PERMISSION: 'permission',
};