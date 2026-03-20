public class ApiResponse<T>
{
    public bool success { get; set; }
    public int statusCode { get; set; }   // ✅ Added
    public string message { get; set; } = "";
    public T? data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success", int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            success = true,
            statusCode = statusCode,
            message = message,
            data = data
        };
    }

    public static ApiResponse<T> Fail(string message, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            success = false,
            statusCode = statusCode,
            message = message,
            data = default
        };
    }
}
