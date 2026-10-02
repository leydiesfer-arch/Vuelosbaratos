using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace VuelosBaratos
{
    record Vuelo(string Origen, string Destino, double Precio);

    class GrafoVuelos
    {
        private readonly Dictionary<string, List<Vuelo>> vuelos =
            new(StringComparer.OrdinalIgnoreCase);

        public void AgregarVuelo(string origen, string destino, double precio)
        {
            if (!vuelos.ContainsKey(origen)) vuelos[origen] = new List<Vuelo>();
            if (!vuelos.ContainsKey(destino)) vuelos[destino] = new List<Vuelo>();

            vuelos[origen].Add(new Vuelo(origen, destino, precio));
        }

        public void CargarArchivo(string archivo)
        {
            foreach (string linea in File.ReadAllLines(archivo))
            {
                string[] d = linea.Split(',');

                if (d.Length != 3 ||
                    !double.TryParse(d[2].Trim(), NumberStyles.Float,
                                     CultureInfo.InvariantCulture, out double precio) ||
                    precio < 0)
                {
                    Console.WriteLine($"Línea inválida omitida: \"{linea}\"");
                    continue;
                }

                AgregarVuelo(d[0].Trim(), d[1].Trim(), precio);
            }
        }

        public void MostrarReporte()
        {
            Console.WriteLine("\n--- REPORTE DE VUELOS ---");

            foreach (var ciudad in vuelos)
                foreach (Vuelo v in ciudad.Value)
                    Console.WriteLine($"{v.Origen} -> {v.Destino} | ${v.Precio:F2}");
        }

        // Dijkstra: válido porque los precios no son negativos.
        public void BuscarVueloBarato(string origen, string destino)
        {
            Console.WriteLine($"\nRuta de {origen} a {destino}");

            if (!vuelos.ContainsKey(origen) || !vuelos.ContainsKey(destino))
            {
                Console.WriteLine("Alguna de las ciudades no existe.");
                return;
            }

            var costos = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var anteriores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string ciudad in vuelos.Keys)
                costos[ciudad] = double.PositiveInfinity;

            costos[origen] = 0;

            var cola = new PriorityQueue<string, double>();
            cola.Enqueue(origen, 0);

            while (cola.TryDequeue(out string? actual, out double costoActual))
            {
                if (costoActual > costos[actual]) continue;
                if (actual.Equals(destino, StringComparison.OrdinalIgnoreCase)) break;

                foreach (Vuelo v in vuelos[actual])
                {
                    double nuevo = costoActual + v.Precio;

                    if (nuevo < costos[v.Destino])
                    {
                        costos[v.Destino] = nuevo;
                        anteriores[v.Destino] = actual;
                        cola.Enqueue(v.Destino, nuevo);
                    }
                }
            }

            if (double.IsPositiveInfinity(costos[destino]))
            {
                Console.WriteLine("No existe una ruta disponible.");
                return;
            }

            var ruta = new List<string>();
            string paso = destino;

            while (!paso.Equals(origen, StringComparison.OrdinalIgnoreCase))
            {
                ruta.Add(paso);
                paso = anteriores[paso];
            }

            ruta.Add(origen);
            ruta.Reverse();

            Console.WriteLine($"Trayecto: {string.Join(" -> ", ruta)}");
            Console.WriteLine($"Costo mínimo: ${costos[destino]:F2}");
        }
    }

    class Program
    {
        static void Main()
        {
            var sistema = new GrafoVuelos();

            sistema.CargarArchivo("vuelos.txt");
            sistema.MostrarReporte();

            Console.Write("\nCiudad de origen: ");
            string origen = (Console.ReadLine() ?? "").Trim();

            Console.Write("Ciudad de destino: ");
            string destino = (Console.ReadLine() ?? "").Trim();

            var cronometro = Stopwatch.StartNew();
            sistema.BuscarVueloBarato(origen, destino);
            cronometro.Stop();

            Console.WriteLine($"\nTiempo de ejecución: {cronometro.Elapsed.TotalMilliseconds:F4} ms");
        }
    }
}