using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.ViewModels
{
    public class MustBeTrueAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
            => value is bool b && b;
    }
}