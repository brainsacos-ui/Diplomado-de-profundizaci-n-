// File: Program.cs
// .NET 7 console app: Linux Q&A Searcher (single-answer searcher)
// Usage: dotnet run -- (or build and run)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LinuxQASearcher
{
    public class QA
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    class Program
    {
        static List<QA> QAs = new List<QA>();

        static void Main(string[] args)
        {
            Console.WriteLine("Linux Q&A Searcher - una sola respuesta por pregunta\n");

            // Load embedded Questions.json if present in working directory; otherwise use built-in fallback
            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Questions.json");
            if (File.Exists(jsonPath))
            {
                try
                {
                    var text = File.ReadAllText(jsonPath);
                    QAs = JsonSerializer.Deserialize<List<QA>>(text);
                    Console.WriteLine($"Cargadas {QAs.Count} preguntas desde Questions.json\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error cargando Questions.json: " + ex.Message);
                    LoadFallback();
                }
            }
            else
            {
                LoadFallback();
            }

            while (true)
            {
                Console.WriteLine("Ingrese la pregunta (escriba 'salir' para terminar, o 'listar' para ver preguntas):");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;
                input = input.Trim();
                if (input.Equals("salir", StringComparison.OrdinalIgnoreCase)) break;
                if (input.Equals("listar", StringComparison.OrdinalIgnoreCase))
                {
                    ListQuestions();
                    continue;
                }

                var answer = FindAnswer(input);
                Console.WriteLine("\nRespuesta: \n" + answer + "\n");
            }

            Console.WriteLine("Saliendo. ¡Éxito!");
        }

        static void ListQuestions()
        {
            Console.WriteLine("Preguntas disponibles:");
            for (int i = 0; i < QAs.Count; i++)
            {
                Console.WriteLine($"{i+1}. {QAs[i].Question}");
            }
            Console.WriteLine();
        }

        static string Normalize(string s)
        {
            if (s == null) return string.Empty;
            s = s.ToLowerInvariant();
            s = Regex.Replace(s, "[¿?!.:,;\\"\\'()-]", " ");
            s = Regex.Replace(s, "\\s+", " ").Trim();
            return s;
        }

        static string FindAnswer(string userQuestion)
        {
            var uq = Normalize(userQuestion);

            // 1) Exact normalized match
            foreach (var qa in QAs)
            {
                if (Normalize(qa.Question) == uq) return qa.Answer;
            }

            // 2) Containment (user question contained in stored question or viceversa)
            foreach (var qa in QAs)
            {
                var nQ = Normalize(qa.Question);
                if (nQ.Contains(uq) || uq.Contains(nQ)) return qa.Answer;
            }

            // 3) Best fuzzy match using Levenshtein distance on normalized strings
            int bestIdx = -1;
            int bestDist = int.MaxValue;
            for (int i = 0; i < QAs.Count; i++)
            {
                var dist = Levenshtein(Normalize(QAs[i].Question), uq);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            // Threshold: accept if reasonably similar
            if (bestIdx >= 0)
            {
                // relative threshold: allow up to ~40% of length
                var targetLen = Math.Max(Normalize(QAs[bestIdx].Question).Length, uq.Length);
                if (targetLen > 0 && bestDist <= targetLen * 0.4)
                {
                    return QAs[bestIdx].Answer + "\n\n(Respuesta aproximada basada en coincidencia de texto)";
                }
            }

            return "No encontré una respuesta precisa — intenta escribir la pregunta con más palabras clave.";
        }

        // Levenshtein distance (iterative, memory efficient)
        static int Levenshtein(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            if (a.Length < b.Length)
            {
                var tmp = a; a = b; b = tmp;
            }

            var previous = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) previous[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                var current = new int[b.Length + 1];
                current[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    var insert = current[j - 1] + 1;
                    var delete = previous[j] + 1;
                    var replace = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                    current[j] = Math.Min(Math.Min(insert, delete), replace);
                }
                previous = current;
            }
            return previous[b.Length];
        }

        static void LoadFallback()
        {
            // If Questions.json not found, load the built-in list based on user provided pairs
            var data = @"[
  {\"Question\": \"¿Cuál es la diferencia entre ls, ls -l y ls -a?\", \"Answer\": \"ls lista archivos, ls -l muestra detalles y ls -a incluye archivos ocultos.\"},
  {\"Question\": \"¿Para qué sirve el comando pwd?\", \"Answer\": \"Muestra la ruta del directorio actual.\"},
  {\"Question\": \"¿cual es el demonio que gestiona los servicios de Linux?\", \"Answer\": \"systemd\"},
  {\"Question\": \"Cual es el comando utilizado para mostrar el directorio de trabajo actual en la terminal de Linux?\", \"Answer\": \"pwd\"},
  {\"Question\": \"¿cual es el comando utilizado para enumerar los archivos en un directorio en Linux?\", \"Answer\": \"ls\"},
  {\"Question\": \"¿Cuál es el comando para mostrar información de uso de memoria en Linux?\", \"Answer\": \"free\"},
  {\"Question\": \"¿cual es el comando para iniciar un servicio en Linux usando systemd?\", \"Answer\": \"systemctl start <nombre-del-servicio>\"},
  {\"Question\": \"¿cual es el comando para enumerar todos los grupos en Linux?\", \"Answer\": \"cat /etc/group\"},
  {\"Question\": \"¿cual es el comando para detener un servicio en Linux usando systemd?\", \"Answer\": \"systemctl stop <nombre-del-servicio>\"},
  {\"Question\": \"Cual es el demonio responsable de montar dispositivos automáticamente en Linux?\", \"Answer\": \"autofs\"},
  {\"Question\": \"¿cual es la base de la filosofía del software libre de Linux?\", \"Answer\": \"Compartir y colaborar\"},
  {\"Question\": \"Cual es el comando para mover un archivo a otro directorio en Linux?\", \"Answer\": \"mv\"},
  {\"Question\": \"¿cual es el archivo que contiene la configuración de resolución de pantalla en Linux?\", \"Answer\": \"/etc/X11/xorg.conf\"},
  {\"Question\": \"¿cual es el archivo que almacena las contraseñas de los usuarios en Linux?\", \"Answer\": \"/etc/shadow\"},
  {\"Question\": \"¿cual es el comando para copiar un archivo en Linux?\", \"Answer\": \"cp\"},
  {\"Question\": \"¿cual es el archivo que contiene la configuración del entorno global para todos los usuarios?\", \"Answer\": \"/etc/profile\"},
  {\"Question\": \"¿cual es el comando para enumerar todos los procesos que se ejecutan en el sistema en Linux?\", \"Answer\": \"ps\"},
  {\"Question\": \"¿Cual es el archivo o ruta que contiene información sobre dispositivos de hardware en Linux?\", \"Answer\": \"/sys/devices\"},
  {\"Question\": \"¿cual es el comando para cambiar el directorio actual a un directorio especifico en Linux?\", \"Answer\": \"cd\"},
  {\"Question\": \"¿cual es el archivo que contiene la configuración de la impresora en Linux?\", \"Answer\": \"/etc/cups\"},
  {\"Question\": \"¿cual es el archivo que contiene la configuración de red para systemd en Linux?\", \"Answer\": \"No existe un archivo único; la configuración de red para systemd se encuentra en /etc/systemd/network/\"},
  {\"Question\": \"¿cual es el comando para mostrar información sobre dispositivos de red en Linux?\", \"Answer\": \"ifconfig\"},
  {\"Question\": \"¿cual es el comando para eliminar un archivo en Linux?\", \"Answer\": \"rm\"},
  {\"Question\": \"¿cual es la utilidad que se utiliza para comprimir archivos en formato .tar.gz en Linux?\", \"Answer\": \"tar\"},
  {\"Question\": \"¿cual es el comando para iniciar un programa en Linux desde la terminal?\", \"Answer\": \"Escribir el nombre del programa\"},
  {\"Question\": \"¿cual es el comando que se utiliza para cambiar los permisos de un archivo en Linux?\", \"Answer\": \"chmod\"},
  {\"Question\": \"¿cual es el archivo que contiene la información sobre los paquetes instalados en Linux (en sistemas basados en debian)?\", \"Answer\": \"/var/lib/dpkg/status\"},
  {\"Question\": \"¿cual es el comando para formatear una partición en Linux?\", \"Answer\": \"mkfs\"},
  {\"Question\": \"¿cual es el comando para crear un nuevo directorio en Linux?\", \"Answer\": \"mkdir\"},
  {\"Question\": \"¿cual es el comando que se utiliza para cambiar la contraseña de un usuario en Linux?\", \"Answer\": \"passwd\"},
  {\"Question\": \"¿cual es el comando para montar una partición en Linux?\", \"Answer\": \"mount\"},
  {\"Question\": \"¿cual es el comando para cambiar el grupo de un archivo en Linux?\", \"Answer\": \"chgrp\"},
  {\"Question\": \"¿cual es el comando para mostrar información de configuración de red en Linux?\", \"Answer\": \"ifconfig\"},
  {\"Question\": \"¿cual es el comando para matar un proceso especifico en Linux?\", \"Answer\": \"kill\"},
  {\"Question\": \"¿cual es el comando para cambiar el nombre de un archivo en Linux?\", \"Answer\": \"mv\"},
  {\"Question\": \"¿cuál es el comando que se utiliza para mostrar información sobre las particiones del sistema en Linux?\", \"Answer\": \"lsblk\"},
  {\"Question\": \"¿cuál es el comando para enumerar todos los archivos en un directorio y sus subcarpetas en Linux?\", \"Answer\": \"ls -R\"},
  {\"Question\": \"¿cuál es el archivo que contiene configuración de hosts en Linux?\", \"Answer\": \"/etc/hosts\"},
  {\"Question\": \"¿cuál es el comando para enumerar todos los paquetes instalados en Linux (usando dpkg)?\", \"Answer\": \"dpkg -l\"},
  {\"Question\": \"¿cómo se obtiene la información de configuración de hardware en Linux?\", \"Answer\": \"lshw\"},
  {\"Question\": \"¿cuál es el archivo que almacena los permisos de un directorio en Linux?\", \"Answer\": \"No existe un archivo especifico; los permisos se almacenan en el sistema de archivos\"},
  {\"Question\": \"¿cuál es el comando para cambiar el propietario de un archivo en Linux?\", \"Answer\": \"chown\"},
  {\"Question\": \"¿cuál es el comando para instalar un paquete de Linux usando el sistema de administración de paquetes dpkg?\", \"Answer\": \"dpkg -i <paquete.deb>\"},
  {\"Question\": \"¿cuál es el comando para enumerar los paquetes instalados en una distribución de Linux?\", \"Answer\": \"dpkg -l\"},
  {\"Question\": \"¿cuál es el comando para agregar un usuario a un grupo de Linux?\", \"Answer\": \"usermod -aG <grupo> <usuario>\"},
  {\"Question\": \"¿cuál es el comando para mostrar información sobre la utilización de la CPU en Linux?\", \"Answer\": \"top\"},
  {\"Question\": \"¿cuál es el comando para otorgar permisos de escritura, lectura y ejecución a un archivo en Linux?\", \"Answer\": \"chmod 777 <archivo>\"},
  {\"Question\": \"¿Cuál es el comando utilizado para crear un nuevo usuario en Linux?\", \"Answer\": \"adduser\"},
  {\"Question\": \"¿Qué comando muestra el espacio libre en disco?\", \"Answer\": \"df -h\"},
  {\"Question\": \"¿Dónde se almacenan los logs del sistema?\", \"Answer\": \"/var/log\"},
  {\"Question\": \"¿Qué hace el comando tar?\", \"Answer\": \"Comprime y descomprime archivos\"},
  {\"Question\": \"¿Qué es el kernel?\", \"Answer\": \"Núcleo del sistema operativo\"}
]";

            try
            {
                QAs = JsonSerializer.Deserialize<List<QA>>(data);
                Console.WriteLine($"Cargadas {QAs.Count} preguntas (fallback integrado).\n");
            }
            catch
            {
                QAs = new List<QA>();
            }
        }
    }
}

/*
File: Questions.json
(opcional - si quieres editar preguntas/respuestas sin recompilar)

Paste exactly the JSON array used in the fallback into a file named Questions.json
and place it in the same folder as the compiled application (or the project root when using dotnet run).

*/

/*
File: README.md

# Linux Q&A Searcher (C#)

Consola .NET 7 app que busca entre preguntas y devuelve una sola respuesta verdadera por pregunta.

## Requisitos
- .NET 7 SDK o superior: https://dotnet.microsoft.com/

## Ejecutar
1. Guardar `Program.cs` y opcionalmente `Questions.json` (si quieres modificar preguntas sin recompilar).
2. `dotnet run` (desde la carpeta del proyecto) o `dotnet build` y ejecutar el binario.

## Modo de uso
- Escribe la pregunta en la consola y presiona Enter.
- Comandos útiles: `listar` para mostrar todas las preguntas, `salir` para terminar.

## Sugerencias
- Para agregar o editar preguntas, modifica `Questions.json` (es un array de objetos `{ Question, Answer }`).
- El buscador intenta coincidencia exacta, búsqueda por contención y una coincidencia difusa basada en Levenshtein.

*/
