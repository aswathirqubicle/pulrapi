
using System.ComponentModel.DataAnnotations;

namespace Core.Application.Security.Validation.Attributes
{
    public class StringLengthCheckAttribute : ValidationAttribute
    {
        public int MinStringLength { get; set; }
        public int MaxStringLength { get; set; }

        public bool NullValueAllowed { get; set; } = true;

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return NullValueAllowed;
            }
            var length = value.ToString().Length;

            if (length < MinStringLength || length >= MaxStringLength)
            {
                return false;
            }
            return true;
        }
    }
}
