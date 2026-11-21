namespace Transliterator.Domain.Entities
{
    /// <summary>
    /// Кусок вывода и арабская буква, из которой он получился (D41). Отрисовщик
    /// отмечает графему согласного вместе с удвоением, но без отзвука кальканя
    /// и гласной: подсказка у буквы вывода говорит о самой букве. Буква — базовый
    /// ключ профиля (<c>ع</c>), тот же, что в таблице звуков.
    /// </summary>
    /// <param name="Start">Начало куска в выводе.</param>
    /// <param name="Length">Длина куска; пустая графема куском не становится.</param>
    /// <param name="Letter">Арабская буква.</param>
    /// <param name="Unvowelled">Буква слова без огласовок (D45): страница подсвечивает такое слово.</param>
    public sealed record LetterSpan(int Start, int Length, string Letter, bool Unvowelled = false);
}
