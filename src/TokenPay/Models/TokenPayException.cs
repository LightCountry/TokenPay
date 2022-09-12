namespace TokenPay.Models
{
    public class TokenPayException : Exception
    {
        public TokenPayException(string? message) : base(message)
        {
        }
    }
}
