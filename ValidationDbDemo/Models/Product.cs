using System;
using System.Collections.Generic;
using System.Text;

namespace ValidationDbDemo.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Price { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public string CategoryName => Category?.Name ?? "";
    }
}
