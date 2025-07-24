namespace UpdatePod.Domain.KubernetesUtils;

public class KubernetesObjectNotFoundException(string message) : Exception(message);