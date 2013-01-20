using Nancy;
using System.Dynamic;
using Simple.Data;
using System.Collections.Generic;
using Retlang.Fibers;
using Retlang.Channels;
using System.IO;
using System.Linq;
using System;

namespace Rest.Service
{
    public class HomeModule:BaseModule
    {
        public HomeModule(IDBFactory dbFactory):base(dbFactory ,"/api")
        {
            
            Get["/puntos/{origen}/{empresa}/{id}/{anio}/{extension}"] = x =>
            {
                return GetPuntos(x);
            };

            Get["/movimientos/{origen}/{empresa}/{id}/{anio}/{extension}"] = x =>
            {
                return GetMovimientos(x);
            };

            Get["/puntos/{origen}/{empresa}/{id}/{extension}"] = x =>
            {
                return GetPuntos(x);
            };

            Get["/movimientos/{origen}/{empresa}/{id}/{extension}"] = x =>
            {
                return GetMovimientos(x);
            };


            Get["/importador"] = x =>
            {
                return string.Format("Directiorio de operaciones:{0}",
                    string.Format(@"{0}{1}\", Path.GetDirectoryName(typeof(Bootstrapper).Assembly.CodeBase)
                    .Replace(@"file:\", string.Empty)
                    .Replace("bin", string.Empty),"inbox"));
            };
           
        }

        private Response GetMovimientos(dynamic x)
        {
            if (!DB.Personas.ExistsByNroDocumentoAndCodEmpresa(x.id, x.empresa))
                return HttpStatusCode.NotFound;            

            return GetMovimientosForCustomer(x);            
        }

        private Response GetMovimientosForCustomer(dynamic x)
        {
            Punto persona = DB.Personas.FindByNroDocumentoAndCodEmpresa(x.id, x.empresa);
            var cuenta = DB.Clientes.FindByNroDocAndCodEmpresa(x.id, x.empresa);

            var numero_cuenta = cuenta == null ? persona.Cuenta : cuenta.NroCuenta;

            var anio = x.anio 
                ?  int.Parse(x.anio) : 0 ;
                       
            var doc = GetCorporativo(persona.Cuenta, x.empresa);
            var response = new List<Movimiento>();
            if (((string)x.origen).ToLowerInvariant() == "corporativo")
               response= MovimientosCorporativos(persona.CodEmpresa,persona.Cuenta,doc,anio);
            else
                response = MovimientosParticulares(persona.CodEmpresa, persona.Cuenta, doc,anio);

            if (x.extension == "json")
                return Response.AsJson(response);
            else
                return Response.AsXml(response);
        }

        private IEnumerable<Movimiento> MovimientosParticulares(int empresa,int cuenta,string doc,int anio=0)
        {
            IList<Movimiento> response= DB.Movimientos_Particulares(empresa,cuenta,doc);
            return anio == 0 ? response.ToList<Movimiento>() : response.Where(x => x.Fecha_Compra.Year == anio).ToList<Movimiento>();
            
        }

        private IEnumerable<Movimiento> MovimientosCorporativos(int empresa, int cuenta, string doc,int anio=0)
        {

            List<Movimiento> response = DB.Movimientos_Corporativos(empresa, cuenta, doc);
            return anio == 0 ? response.ToList<Movimiento>() : response.Where(x => x.Fecha_Compra.Year == anio).ToList<Movimiento>();
        }

        private Response GetPuntos(dynamic x)
        {
            if (!DB.Personas.ExistsByNroDocumentoAndCodEmpresa(x.id, x.empresa))
                return HttpStatusCode.NotFound;

            var anio = x.anio
               ? int.Parse(x.anio) : 0;

            Punto response = DB.Personas.FindByNroDocumentoAndCodEmpresa(x.id, x.empresa);

            return GetPuntosForCustomer(x, response,anio);            
        }

        private Response GetPuntosForCustomer(dynamic x, Punto response,int anio=0)
        {
            if (((string)x.origen).ToLowerInvariant() == "corporativo")
                PuntosCorporativos(response,anio);
            else
                PuntosParticulares(response,anio);

           if (x.extension == "json")
                return Response.AsJson(response);
            else
                return Response.AsXml(response);
        }

        private void PuntosCorporativos(Punto response,int anio)
        {
            var doc = GetCorporativo(response.Cuenta, response.CodEmpresa);
            dynamic sumaPuntos, canjePuntos;
            if (anio == 0)
            {
                sumaPuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                   .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                   .Where(DB.Cuentas_Corrientes.Movimiento == 0 || DB.Cuentas_Corrientes.Movimiento == 2)
                   .Where(DB.Cuentas_Corrientes.NroDoc == doc).ToList();

                canjePuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                    .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                    .Where(DB.Cuentas_Corrientes.Movimiento == 1 || DB.Cuentas_Corrientes.Movimiento == 4)
                    .Where(DB.Cuentas_Corrientes.NroDoc == doc).ToList();
            }
            else
            {
                sumaPuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                   .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                   .Where(DB.Cuentas_Corrientes.Movimiento == 0 || DB.Cuentas_Corrientes.Movimiento == 2)
                   .Where(DB.Cuentas_Corrientes.fecha_Compra >= @"01/01/" + anio && DB.Cuentas_Corrientes.fecha_Compra <= @"31/12/" + anio)
                   .Where(DB.Cuentas_Corrientes.NroDoc == doc).ToList();

                canjePuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                    .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                    .Where(DB.Cuentas_Corrientes.fecha_Compra >= @"01/01/" + anio && DB.Cuentas_Corrientes.fecha_Compra <= @"31/12/" + anio)
                    .Where(DB.Cuentas_Corrientes.Movimiento == 1 || DB.Cuentas_Corrientes.Movimiento == 4)
                    .Where(DB.Cuentas_Corrientes.NroDoc == doc).ToList() ;                
            }
            Sumador(response, sumaPuntos, canjePuntos);
        }

        private static void Sumador(Punto response, dynamic SumaPuntos, dynamic CanjePuntos)
        {
            var puntosSume = SumaPuntos[0].Puntos == null ? 0 : SumaPuntos[0].Puntos;
            var puntosCanje = CanjePuntos[0].Puntos == null ? 0 : CanjePuntos[0].Puntos;

            var montoSuma = SumaPuntos[0].Monto == null ? 0 : SumaPuntos[0].Monto;
            var montoCanje = CanjePuntos[0].Monto == null ? 0 : CanjePuntos[0].Monto;

            response.Puntos = puntosSume - puntosCanje;
            response.MontoCompra = montoSuma - montoCanje;
        }

        private void PuntosParticulares(Punto response,int anio)
        {
            var doc = GetCorporativo(response.Cuenta, response.CodEmpresa);
            dynamic sumaPuntos, canjePuntos;

            if (anio == 0)
            {
                sumaPuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                    .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                    .Where(DB.Cuentas_Corrientes.Movimiento == 0 || DB.Cuentas_Corrientes.Movimiento == 2)
                    .Where(DB.Cuentas_Corrientes.NroDoc != doc).ToList();

                canjePuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                    .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                    .Where(DB.Cuentas_Corrientes.Movimiento == 1 || DB.Cuentas_Corrientes.Movimiento == 4)
                    .Where(DB.Cuentas_Corrientes.NroDoc != doc).ToList();
            }
            else 
            {
                sumaPuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                    .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                    .Where(DB.Cuentas_Corrientes.Movimiento == 0 || DB.Cuentas_Corrientes.Movimiento == 2)
                    .Where(DB.Cuentas_Corrientes.fecha_Compra >= @"01/01/" + anio && DB.Cuentas_Corrientes.fecha_Compra <= @"31/12/" + anio)
                    .Where(DB.Cuentas_Corrientes.NroDoc != doc).ToList();

                canjePuntos = DB.Cuentas_Corrientes.FindAllByCuentaAndCodEmpresa(response.Cuenta, response.CodEmpresa)
                    .Select(DB.Cuentas_Corrientes.MontoCompra.Sum().As("Monto"), DB.Cuentas_Corrientes.CantidadPuntos.Sum().As("Puntos"))
                    .Where(DB.Cuentas_Corrientes.fecha_Compra >= @"01/01/" + anio && DB.Cuentas_Corrientes.fecha_Compra <= @"31/12/" + anio)
                    .Where(DB.Cuentas_Corrientes.Movimiento == 1 || DB.Cuentas_Corrientes.Movimiento == 4)
                    .Where(DB.Cuentas_Corrientes.NroDoc != doc).ToList();
            }
            Sumador(response, sumaPuntos, canjePuntos);
        }

        private string GetCorporativo(int cuenta, int empresa)
        {
            var corporativo = DB.Personas
                .FindByCodEmpresaAndCuentaAndTipoDocumento(empresa, cuenta, "CUI");
            return corporativo.NroDocumento;
        }        
            
    }    
}