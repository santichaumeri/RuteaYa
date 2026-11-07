using RuteaYa.Entidades;
using RuteaYa.Entidades.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Logica.Dtos.Envios;

public class EnvioDetalleDto
{
    public int Id { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public decimal PesoKg { get; set; }
    public TamanoPaquete Tamano { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal MontoOfrecido { get; set; }
    public EstadoEnvio Estado { get; set; }
}
