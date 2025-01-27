using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FuseBox
{
    public abstract class BaseEntity
    {
        [JsonProperty(Order = 1)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required!")]
        [StringLength(50)]
        [JsonProperty(Order = 2)]
        public string? Name { get; set; }
    }

}
