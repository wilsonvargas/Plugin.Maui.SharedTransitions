namespace SharedTransitions.Sample;

public sealed record Destination(int Id, string Name, string Region, string Emoji, Color Color, string Description)
{
	public static IReadOnlyList<Destination> All { get; } =
	[
		new(1, "Machu Picchu", "Cusco", "⛰️", Color.FromArgb("#2E7D32"), "Ciudadela inca del siglo XV en lo alto de los Andes, rodeada de bosque nuboso y montañas."),
		new(2, "Lago Titicaca", "Puno", "🛶", Color.FromArgb("#1565C0"), "El lago navegable más alto del mundo, hogar de las islas flotantes de los uros."),
		new(3, "Huacachina", "Ica", "🏜️", Color.FromArgb("#EF6C00"), "Un oasis rodeado de dunas gigantes, ideal para sandboard y tubulares al atardecer."),
		new(4, "Cañón del Colca", "Arequipa", "🦅", Color.FromArgb("#6D4C41"), "Uno de los cañones más profundos del planeta, donde se ve volar al cóndor andino."),
		new(5, "Paracas", "Ica", "🐧", Color.FromArgb("#00838F"), "Reserva marina con lobos marinos, pingüinos de Humboldt y las islas Ballestas."),
		new(6, "Kuélap", "Amazonas", "🏰", Color.FromArgb("#5E35B1"), "Fortaleza de los chachapoyas sobre una montaña, más antigua que Machu Picchu."),
		new(7, "Máncora", "Piura", "🏄", Color.FromArgb("#F9A825"), "Playas de arena blanca y olas todo el año en el norte peruano."),
		new(8, "Vinicunca", "Cusco", "🌈", Color.FromArgb("#C2185B"), "La Montaña de Siete Colores, a más de 5 000 metros sobre el nivel del mar."),
	];
}
