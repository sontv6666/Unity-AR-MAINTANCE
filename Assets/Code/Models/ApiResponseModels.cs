using System;
using System.Collections.Generic;
using UnityEngine;
namespace Models 
{
    [Serializable]
    public class ApiResponse<T>
    {
        public int code;
        public T result;
    }

    
    
    
    
    
    // [Serializable]
    // public class ApiResponseList<T>
    // {
    //     public int code;
    //     public PaginationResult<T> result;
    // }
    [Serializable]
    public class ApiResponseList<T>
    {
        public int code;
        public List<T> result;  // ✅ Correctly handles array in "result"
    }

    // ✅ Pagination structure (matches API response)
    [Serializable]
    public class PaginationResult<T>
    {
        public int page;
        public int size;
        public int totalItems;
        public int totalPages;
        public List<T> objectList;
    }
    [Serializable]
    public class CourseListResponse
    {
        public List<CourseResult> objectList;  // ✅ Matches API response structure
    }

    
    
    
    [Serializable]
    public class CourseResult
    {
        public string id;
        public string courseCode;
        public string modelId;
        public string title;
        public string description;
        public string? shortDescription;
        public string? targetAudience;
        public string companyId;
        public string imageUrl;
        public string status;
        public string type;
        public int? duration;
        public bool isMandatory;
        public int? numberOfLessons;
        public int? numberOfParticipants;
        public Instruction[] instructions;
        public int? numberOfStaffScan;
        public string machineTypeId;
       
    }
    [Serializable]
    public class MachineTypeResponse
    {
        public string machineTypeId;
        public string machineTypeName;
        public string description;
        public List<MachineTypeAttributeResponse> machineTypeAttributeResponses;
        public int numOfAttribute;
    }

    [Serializable]
    public class MachineTypeAttributeResponse
    {
        public string id;
        public string modelTypeId;
        public string attributeName;
        public string valueAttribute;
    }

    [Serializable]
    public class MachineResponse
    {
        public string id;
        public string machineName;
        public string machineType;
        public string machineCode;
        public string apiUrl;
        public List<HeaderResponse> headerResponses;
        public string token;
        public string qrCode;
        public List<MachineTypeValueResponse> machineTypeValueResponses;
    }

    [Serializable]
    public class HeaderResponse
    {
        public string keyHeader;
        public string valueOfKey;
    }

    [Serializable]
    public class MachineTypeValueResponse
    {
        public string? id;
        public string machineTypeAttributeId;
        public string machineTypeAttributeName;
        public string valueAttribute;
    }
    
    
    

    [Serializable]
    public class Instruction
    {
        public string id;
        public string courseId;
        public int orderNumber;
        public string name;
        public string description;
        public string position;
        public string rotation;
        public List<InstructionDetail> instructionDetailResponse;
    }
    
    
    [Serializable]
    public class InstructionDetail
    {
        public string id;
        public string instructionId;
        public string name;
        public List<string> meshes;
        public string animationName;
        public int orderNumber;
        public string description;
        public string fileString;
        public string imgString;
    }
    

    [Serializable]
    public class ModelDataResult
    {
        public string id;
        public string modelTypeId;
        public string modelTypeName;
        public string modelCode;
        public string status;
        public string name;
        public string companyId;
        public string description;
        public string imageUrl;
        public bool isUsed;
        public string version;
    
        public string scale;
        public Vector3 GetScale()
        {
            if (!string.IsNullOrEmpty(scale))
            {
                string[] scaleValues = scale.Split(',');

                if (scaleValues.Length == 1) // Single scale value (e.g., "1")
                {
                    if (float.TryParse(scaleValues[0], out float uniformScale))
                    {
                        return Vector3.one * uniformScale; // Apply uniform scale
                    }
                }
                else if (scaleValues.Length == 3) // Separate x, y, z values (e.g., "1,2,3")
                {
                    if (float.TryParse(scaleValues[0], out float x) &&
                        float.TryParse(scaleValues[1], out float y) &&
                        float.TryParse(scaleValues[2], out float z))
                    {
                        return new Vector3(x, y, z);
                    }
                }
            }

            return Vector3.one; // Default scale if parsing fails
        }
        public float[] position;  // Position as [x, y, z]
        public float[] rotation;  // Rotation as [x, y, z]
        public string file;
        public string courseName;
    }
    [Serializable]
    public class LoginRequest
    {
        public string email;
        public string password;
    }
    [Serializable]
    public class LoginResponse
    {
        public int code;
        public string message;
        public LoginResult result;  // ✅ Fix: result now matches API structure
    }
    [Serializable]
    public class LoginResult
    {
        public string token;
        public string message;
        public UserProfileResult user; 
    }
    [Serializable]
    public class UserProfileResult
    {
        public string id;
        public Role role;
        public string roleName;
        public Company company;
        public string email;
        public string currentPlan;
        public string avatar;
        public string username;
        public string deviceId;
        public string phone;
        public string status;
        public string expirationDate;
        public bool isPayAdmin;
        public string createdDate;
        public string updatedDate;
        public int points;
    }

    [Serializable]
    public class Role
    {
        public string id;
        public string roleName;
    }

    [Serializable]
    public class Company
    {
        public string id;
        public string companyName;
    }

    [Serializable]
    public class UpdateUserDeviceRequest
    {
        public string id;
        public string deviceId;
    }

}