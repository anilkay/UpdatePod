namespace UpdatePod.ImageOperations;

public interface IImageOperations
{
    public Task<string> GetLatestHashFromImage(string image);
}