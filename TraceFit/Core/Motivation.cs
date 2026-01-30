namespace TraceFit;

public static class Motivation
{
    public static (string headline, string sub, string badge) CopyForCircleGame(double pct, CircleGameState state)
    {
        // mensagens por estado (evita “mentir” quando a pessoa só rabiscou)
        return state switch
        {
            CircleGameState.Idle =>
                ("Desenhe um círculo livre ✨", "Tente dar a volta completa (360°) e fechar o traço.", "BORA!"),

            CircleGameState.Drawing =>
                ("Vaiiii… continua! 🔥", "Dê a volta e tente fechar o círculo. Capricha no contorno.", "AO VIVO"),

            CircleGameState.TooSmall =>
                ("Maior! 😄", "Faça um círculo mais grande. Círculo pequeno fica impreciso.", "AUMENTA"),

            CircleGameState.NotEnoughCoverage =>
                ("Falta dar a volta! 🌀", "Você precisa cobrir mais do círculo (quase 360°).", "CONTINUA"),

            CircleGameState.ScribblePenalty =>
                ("Rabisco não vale 😅", "Tente um contorno único. Rabisco aumenta o caminho e derruba a nota.", "LIMPA"),

            _ => ScoreCopy(pct)
        };
    }

    private static (string headline, string sub, string badge) ScoreCopy(double pct)
    {
        if (pct < 15) return ("Aquecendo! 🔥", "Tenta um contorno mais limpo e contínuo.", "VAI!");
        if (pct < 35) return ("Boa! 😄", "Feche melhor e evite tremidas no raio.", "BOM!");
        if (pct < 55) return ("Tá ficando redondo! 👏", "Agora dá a volta completa e fecha o círculo.", "SHOW!");
        if (pct < 70) return ("Quase lá! 🚀", "Só mais um ajuste no contorno e fica top!", "QUASE!");
        if (pct < 85) return ("Mandou bem! ⭐", "Isso tá com cara de círculo de verdade!", "TOP!");
        if (pct < 95) return ("Parabéns! 🏆", "Controle absurdo. Tá MUITO próximo do perfeito.", "UAAU!");
        return ("PERFEITO! LENDÁRIO 👑", "Isso foi círculo perfeito. Insano!", "GOD!");
    }
}
