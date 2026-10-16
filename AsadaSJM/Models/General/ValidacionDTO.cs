namespace AsadaSJM.Models.General
{
    public class ValidacionDTO<T>
    {
        public bool EsValido { get; set; }
        public string MensajeValidacion { get; set; } = string.Empty;
        public T? Datos { get; set; }

        public static ValidacionDTO<T> Exito(T datos, string mensaje = "Operación realizada correctamente.")
        {
            return new ValidacionDTO<T>
            {
                EsValido = true,
                MensajeValidacion = mensaje,
                Datos = datos
            };
        }

        public static ValidacionDTO<T> Fallo(string mensaje)
        {
            return new ValidacionDTO<T>
            {
                EsValido = false,
                MensajeValidacion = mensaje,
                Datos = default
            };
        }
    }
}
