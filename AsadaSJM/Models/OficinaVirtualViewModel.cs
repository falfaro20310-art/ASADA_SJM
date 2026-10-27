using System;
using System.Collections.Generic;

namespace AsadaSJM.Models;

// modelo para la vista de la Oficina Virtual del Abonado 

public class OficinaVirtualViewModel
{
    public int IdAbonado { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    public string NIS { get; set; } = string.Empty;

    public string NumeroPaja { get; set; } = string.Empty;

    public string NumeroFinca { get; set; } = string.Empty;

    public string NumeroPlano { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    public string Tarifa { get; set; } = string.Empty;

    public string Sector { get; set; } = string.Empty;
}

// Modelo para la vista de los recibos del abonado en la Oficina Virtual

public class ReciboAbonadoViewModel
{
    public int IdRecibo { get; set; }

    public string NIS { get; set; } = string.Empty;

    public string NumeroPaja { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    public string Periodo { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public DateTime FechaEmision { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public string? RutaPDF { get; set; }

    public string Estado { get; set; } = string.Empty;
}

// Modelo para la vista de los recibos pendientes del abonado en la Oficina Virtual

public class ReciboPendienteViewModel
{
    public int IdRecibo { get; set; }

    public string NIS { get; set; } = string.Empty;

    public string Periodo { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public string Estado { get; set; } = string.Empty;
}

// Modelo para la vista del consumo del abonado en la Oficina Virtuall

public class ConsumoAbonadoViewModel
{
    public int IdConsumo { get; set; }

    public string NIS { get; set; } = string.Empty;

    public string Periodo { get; set; } = string.Empty;

    public decimal MetrosCubicos { get; set; }

    public DateTime FechaLectura { get; set; }
}

// Modelo para la vista de los consumos del abonado en la Oficina Virtual con filtro de fechas 

public class FiltroConsumoViewModel
{
    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public List<ConsumoAbonadoViewModel> Consumos { get; set; } = new();
}
