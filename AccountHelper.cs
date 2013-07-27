using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;

namespace Rest.Service
{
    public static class AccountHelper
    {

        internal static Response GetPuntos(dynamic x,dynamic DB)
        {
            if (!DB.Personas.ExistsByNroDocumentoAndCodEmpresa(x.id, x.empresa))
                return HttpStatusCode.NotFound;

            return GetPuntosForCustomer(x);

        }

        private static Response GetPuntosForCustomer(dynamic x)
        {
            throw new NotImplementedException();
        }

        internal static Response GetMovimientos(dynamic x,dynamic DB)
        {
            if (!DB.Personas.ExistsByNroDocumentoAndCodEmpresa(x.id, x.empresa))
                return HttpStatusCode.NotFound;

            return GetMovimientosForCustomer(x,DB);
        }

        private static Response GetMovimientosForCustomer(dynamic x,dynamic DB)
        {
            Punto persona = DB.Personas.FindByNroDocumentoAndCodEmpresa(x.id, x.empresa);
            var cuenta = DB.Clientes.FindByNroDocAndCodEmpresa(x.id, x.empresa);

            var numero_cuenta = cuenta == null ? persona.Cuenta : cuenta.NroCuenta;

            var response = new List<Movimiento>();


        }
    }
}