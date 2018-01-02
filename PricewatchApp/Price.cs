namespace PricewatchApp
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Price
    {
        [Column("Price")]
        [Required]
        [StringLength(512)]
        public string Price1 { get; set; }

        [Key]
        [Column(Order = 0)]
        public DateTime Date { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(512)]
        public string App { get; set; }

        public virtual App App1 { get; set; }
    }
}
