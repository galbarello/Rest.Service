using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nancy;
using System;

namespace Rest.Service
{
    public static class AccountHelper
    {

        internal static Punto GetPuntos(dynamic x,dynamic DB)
        {
            if (!DB.Personas.ExistsByNroDocumentoAndCodEmpresa(x.id, x.empresa))
                return null;

            return GetPuntosForCustomer(x,DB);

        }

        private static Punto GetPuntosForCustomer(dynamic x,dynamic DB)
        {
            Punto persona = DB.Personas.FindByNroDocumentoAndCodEmpresa(x.id, x.empresa);
            var cuenta = DB.Clientes.FindByNroDocAndCodEmpresa(x.id, x.empresa);

           var sumaPuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(persona.Cuenta, persona.CodEmpresa)
                   .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                   .Where(DB.Cuentas_Corrientes.Movimiento == 0 || DB.Cuentas_Corrientes.Movimiento == 2)
                   .ToList();

            var canjePuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(persona.Cuenta, persona.CodEmpresa)
                .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                .Where(DB.Cuentas_Corrientes.Movimiento == 1 || DB.Cuentas_Corrientes.Movimiento == 4)
                .ToList();

            return Sumador(persona,sumaPuntos, canjePuntos);
        }

        internal static IList<Movimiento> GetMovimientos(dynamic x, dynamic DB)
        {
            if (!DB.Personas.ExistsByNroDocumentoAndCodEmpresa(x.id, x.empresa))
                return null;

            return GetMovimientosForCustomer(x,DB);
        }

        private static IList<Movimiento> GetMovimientosForCustomer(dynamic x, dynamic DB)
        {
            Punto persona = DB.Personas.FindByNroDocumentoAndCodEmpresa(x.id, x.empresa);
            var cuenta = DB.Clientes.FindByNroDocAndCodEmpresa(x.id, x.empresa);

            var numero_cuenta = cuenta == null ? persona.Cuenta : cuenta.NroCuenta;

            List<Movimiento> response = DB.Movimientos_cuenta(persona.CodEmpresa,persona.Cuenta,DateTime.MinValue);

            return response;
        }

        private static Punto Sumador(Punto response, dynamic SumaPuntos, dynamic CanjePuntos)
        {
            var puntosSume = SumaPuntos[0].Puntos == null ? 0 : SumaPuntos[0].Puntos;
            var puntosCanje = CanjePuntos[0].Puntos == null ? 0 : CanjePuntos[0].Puntos;

            var montoSuma = SumaPuntos[0].Monto == null ? 0 : SumaPuntos[0].Monto;
            var montoCanje = CanjePuntos[0].Monto == null ? 0 : CanjePuntos[0].Monto;

            response.Puntos = puntosSume - puntosCanje;
            response.MontoCompra = montoSuma - montoCanje;

            return response;
        }
    }
}