using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models.Contacts
{
    public class ContactModel
    {
        [Key]
        public int Id { get; set; }
        [Column(TypeName = "nvarchar")]
        [StringLength(50)]
        [Required(ErrorMessage = "Phải nhập {0}")]
        [DisplayName("Họ tên")]
        public string FullName { get; set; }
        [StringLength(100)]
        [Required(ErrorMessage = "Phải nhập {0}")]
        [EmailAddress(ErrorMessage = "Phải là địa chỉ Email")]
        [DisplayName("Địa chỉ Email")]
        public string Email { get; set; }
        public DateTime DateSent { get; set; }
        [DisplayName("Nội dung")]
        public string Message { get; set; }
        [StringLength(50)]
        [Phone(ErrorMessage = "Phải là số điện thoại")]
        [DisplayName("Số điện thoại")]
        public string Phone { get; set; }
    }
}
//dotnet aspnet-codegenerator controller -name Contact -namespace App.Areas.Contact.Controllers  -m App.Models.Contacts.ContactModel -udl -dc App.Models.AppDbContect -outDir Areas/Contact/Controllers/  