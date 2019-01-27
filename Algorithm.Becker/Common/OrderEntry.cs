using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace QuantConnect.Algorithm.Becker
{
    class OrderEntry
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public decimal Qty { get; set; }
    }
}