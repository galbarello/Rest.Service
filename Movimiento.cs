using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rest.Service
{
    [Serializable]
    public class Movimiento
    {
        public DateTime Fecha_Compra { get; set; }
        public DateTime Hora_Compra { get; set; }
        public string Apellido { get; set; }
        public string Nombre { get; set; }
        public double MontoCompra { get; set; }
        public double CantidadPuntos { get; set; }
        public string Operador { get; set; }
        public string Comprobante { get; set; }
        public string DescMovimiento { get; set; }
        public string Sucursal { get; set; }

    }
}
