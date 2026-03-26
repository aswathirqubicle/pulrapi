using System.ComponentModel.DataAnnotations;

namespace Core.Application.Security.Validation.Attributes
{
    public class QueryOrder : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value != null && !(value.ToString().ToLower() == "asc" || value.ToString().ToLower() == "desc"))
                return false;

            return true;
        }
    }
}
