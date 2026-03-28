using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models.DTOs
{
    public class DispenseRecord
    {
        public int PresentationId { get; set; }
        public int FacilityId { get; set; }
        public int DispensedById { get; set; }
    }
}
