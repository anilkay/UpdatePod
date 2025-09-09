namespace UpdatePod.Domain.PodUpdateOperations;

public class InvalidImagePullPolicyException: Exception
{
    public InvalidImagePullPolicyException(string actual, string expected)
        : base($"Image pull policy must be {expected} but {actual}") {}   
}