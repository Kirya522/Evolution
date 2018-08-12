﻿using System;
using UnityEngine;

[Serializable]
public class Bug
{
    #region Constants

    /// <summary>
    ///     Количество жизней, с которым появляется жук
    /// </summary>
    public static readonly int StartBugHealth = 50;

    /// <summary>
    ///     Максимальное количество жизней у жука
    /// </summary>
    public static readonly int MaxBugHealth = 256;

    /// <summary>
    ///     Количество жизней, на которое уменьшается жизнь жука-родителя при генерации нового жука
    /// </summary>
    public static readonly int MuptiplyCost = 30;

    /// <summary>
    ///     Количество жизней, которое получаает жук, съедая минеральную ягоду
    /// </summary>
    public static readonly int MineralBerryValue = 30;

    /// <summary>
    ///     Количество жизней, которое получаает жук, съедая обычную ягоду
    /// </summary>
    public static readonly int BerryValue = 10;

    #endregion

    #region Constructors
    // Зачем здесь логика на спавн???
    public Bug(Color bugColor, Genome genome = null, Coordinates currentPosition = null)
    {
        if (currentPosition == null)
            do
            {
                currentPosition = Coordinates.RandomCoordinates(Map.Size.Y, Map.Size.X);
            } while (Data.WorldMap[currentPosition.Y, currentPosition.X].CellType != CellEnum.TypeOfCell.Empty);

        CurrentPosition = currentPosition;
        Data.WorldMap[currentPosition.Y, currentPosition.X].CellType = CellEnum.TypeOfCell.Bug;
        if (genome == null)
            Gene = new Genome();
        else
            Gene = genome;

        Health = StartBugHealth;
        LifeTime = 0;
        Gene.CurrentGenePosition = 0;
        Direction = Data.Rnd.Next(0, 8);
        color = bugColor;
        Data.WorldMap[currentPosition.Y, currentPosition.X].LinkedBug = this;
    }


    #endregion

    #region Properties

    private Color _color;

    public Color color
    {
        get
        {
            return _color;
        }

        set
        {
            _color = value;

            if (value.b < 0)
            {
                _color.b = 0;
            }

            if (value.b > 1)
            {
                _color.b = 1;
            }

            if (value.g < 0)
            {
                _color.g = 0;
            }

            if (value.g > 1)
            {
                _color.g = 1;
            }

            if (value.r < 0)
            {
                _color.r = 0;
            }

            if (value.r > 1)
            {
                _color.r = 1;
            }
        }
    }

    public int Direction { get; set; }

    public int LifeTime { get; set; }

    private int _health;

    public int Health
    {
        get { return _health; }

        set
        {
            if (value > -1 && value <= MaxBugHealth)
                _health = value;
            else
                _health = 0;
        }
    }

    public Genome Gene { get; set; }

    #endregion

    #region Position

    public Coordinates LastPosition { get; set; }

    public Coordinates CurrentPosition { get; set; }

    private static Cell DestinationCell;

    #endregion

    /// <summary>
    ///     Массив делегатов, который хранит команды жука
    /// </summary>
    public void StartAction()
    {
        LastPosition = CurrentPosition;
        int countSteps = 0;
        bool isEnd = false;
        Health--;
        LifeTime++;
        while (countSteps < BugCollection.MaxStepsBug && !isEnd)
        {
            isEnd = DoCommand(this);
            countSteps++;
        }

        // Убить жука, если он превысил допустимое количество команд за один ход
        if (countSteps == BugCollection.MaxStepsBug && !isEnd) Health = 0;
    }
    

    /// <summary>
    ///     Считает промежуточные данные, необходимые для выполнения дальнейших команд
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    public static bool DoCommand(Bug bug)
    {
        Coordinates destination =
            Coordinates.CoordinateShift[CalculateShift(bug)] + bug.CurrentPosition;
        DestinationCell = Data.WorldMap[destination.Y, destination.X];

        // Если данному номеру генома присвоена команда, тогда необходимо её выполнить
        if (MasBugCommands.Length > bug.Gene.genome[bug.Gene.CurrentGenePosition])
        {
            return MasBugCommands[bug.Gene.genome[bug.Gene.CurrentGenePosition]].Invoke(bug);
        }
        else
        {
            // Иначе сделать переход по геному
            return GenomJump(bug);
        }
    }

    /// <summary>
    ///     Делегат, аналогичный всем командам жука, необходим для хранения той или иной команды
    /// </summary>
    public delegate bool BugCommand(Bug bug);

    /// <summary>
    ///     Массив делегатов, которые хранят команды жука
    /// </summary>
    public static BugCommand[] MasBugCommands = {Move, Rotate, CheckCell, Take, Multiply, Push, CheckHealth, Attack, Share};

    private static int CalculateShift(Bug bug)
    {
        int res = 0;

        // С учетом текущего поворота
        res = (bug.Direction + bug.Gene.genome[bug.Gene.NextGenePosition()]) % 8;
        return res;
    }

    #region BugCommands

    /// <summary>
    ///     Жук пытается сходить на определённую клетку рядом с собой
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    private static bool Move(Bug bug)
    {
        bug.Gene.CurrentGenePosition += (int) DestinationCell.CellType + 1;
        var neighbourBug = DestinationCell.LinkedBug;
        if (neighbourBug != null && neighbourBug.IsFriendBug(bug)) bug.Gene.CurrentGenePosition++;

        switch (DestinationCell.CellType)
        {
            case CellEnum.TypeOfCell.Empty:
            {
                Data.WorldMap[bug.CurrentPosition.Y, bug.CurrentPosition.X].LinkedBug = null;
                Data.WorldMap[bug.CurrentPosition.Y, bug.CurrentPosition.X].CellType = CellEnum.TypeOfCell.Empty;
                bug.CurrentPosition = DestinationCell.Coordinate;
                DestinationCell.CellType = CellEnum.TypeOfCell.Bug;
                DestinationCell.LinkedBug = bug;
                break;
            }

            case CellEnum.TypeOfCell.Berry:
            {
                Data.WorldMap[bug.CurrentPosition.Y, bug.CurrentPosition.X].LinkedBug = null;
                Data.WorldMap[bug.CurrentPosition.Y, bug.CurrentPosition.X].CellType = CellEnum.TypeOfCell.Empty;
                bug.CurrentPosition = DestinationCell.Coordinate;
                DestinationCell.CellType = CellEnum.TypeOfCell.Bug;
                DestinationCell.LinkedBug = bug;
                bug.Health += BerryValue;
                bug.color = new Color(bug.color.r - 0.1f, bug.color.g + 0.1f, bug.color.b - 0.1f);
                    break;
            }

            case CellEnum.TypeOfCell.MineralBerry:
            {
                Data.WorldMap[bug.CurrentPosition.Y, bug.CurrentPosition.X].LinkedBug = null;
                Data.WorldMap[bug.CurrentPosition.Y, bug.CurrentPosition.X].CellType = CellEnum.TypeOfCell.Empty;
                bug.CurrentPosition = DestinationCell.Coordinate;
                DestinationCell.CellType = CellEnum.TypeOfCell.Bug;
                DestinationCell.LinkedBug = bug;
                bug.Health += MineralBerryValue;
                bug.color = new Color(bug.color.r - 0.1f, bug.color.g - 0.1f, bug.color.b + 0.1f);
                    break;
            }

            case CellEnum.TypeOfCell.Poison:
            {
                bug.Health = 0;
                break;
            }
        }

        return true;
    }

    /// <summary>
    ///     Жук проверяет определённую клетку рядом с собой
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    private static bool CheckCell(Bug bug)
    {
        bug.Gene.CurrentGenePosition += (int) DestinationCell.CellType + 1;
        var neighbourBug = DestinationCell.LinkedBug;
        if (neighbourBug != null && neighbourBug.IsFriendBug(bug)) bug.Gene.CurrentGenePosition++;

        return false;
    }

    /// <summary>
    ///     Жук пытается взять содержимое определённой клетки рядом с собой
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    private static bool Take(Bug bug)
    {
        bug.Gene.CurrentGenePosition += (int) DestinationCell.CellType + 1;
        var neighbourBug = DestinationCell.LinkedBug;
        if (neighbourBug != null && neighbourBug.IsFriendBug(bug)) bug.Gene.CurrentGenePosition++;

        switch (DestinationCell.CellType)
        {
            case CellEnum.TypeOfCell.Berry:
                {
                    DestinationCell.CellType = CellEnum.TypeOfCell.Empty;
                    bug.Health += BerryValue;
                    bug.color = new Color(bug.color.r - 0.1f, bug.color.g + 0.1f, bug.color.b - 0.1f);
                    break;
                }

            case CellEnum.TypeOfCell.MineralBerry:
                {
                    DestinationCell.CellType = CellEnum.TypeOfCell.Empty;
                    bug.Health += MineralBerryValue;
                    bug.color = new Color(bug.color.r - 0.1f, bug.color.g - 0.1f, bug.color.b + 0.1f);
                    break;
                }

            case CellEnum.TypeOfCell.Poison:
                {
                    DestinationCell.CellType = CellEnum.TypeOfCell.Berry;
                    break;
                }
        }

        return true;
    }

    /// <summary>
    ///     Комадна сравнивает геномы текущего и переданного жуков, если геном отличается больше чем на 1 ячейку, считается, что жуки не родственники
    /// </summary>
    /// <param name="bug"> Жук, которого надо проверить на родство с нашим  </param>
    private bool IsFriendBug(Bug bug)
    {
        var isDifference = false;
        for (var i = 0; i < Gene.genome.Length; i++)
            if (Gene.genome[i] != bug.Gene.genome[i])
            {
                if (isDifference)
                    return false;
                isDifference = true;
            }

        return true;
    }

    /// <summary>
    /// Жук поворачивается определённое количество раз
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    private static bool Rotate(Bug bug)
    {
        bug.Direction = bug.Direction + bug.Gene.genome[bug.Gene.NextGenePosition()] % 8;
        bug.Gene.CurrentGenePosition += 2;
        return false;
    }

    /// <summary>
    /// Жук создает одного потомка рядом с собой
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    private static bool Multiply(Bug bug)
    {
        bug.Health -= MuptiplyCost;
        var isBorn = false;
        for (var i = 0; i < 8 && !isBorn; i++)
        {
            var birthCoordinate = bug.CurrentPosition + Coordinates.CoordinateShift[i];
            if (Data.WorldMap[birthCoordinate.Y, birthCoordinate.X].CellType == CellEnum.TypeOfCell.Empty)
            {
                Bug childBug = new Bug(bug.color,
                    new Genome(bug.Gene.GenomeMutate(Data.Rnd.Next(0, 2))),
                    birthCoordinate);
                ControlScript.childs.Add(childBug);
                isBorn = true;
                childBug.Health = bug.Health / 2;
            }
        }

        bug.Gene.CurrentGenePosition++;
        bug.Health = bug.Health / 2;
        return true;
    }

    /// <summary>
    /// Жук пытается толкнуть содержимое определённой клетки рядом с собой
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    private static bool Push(Bug bug)
    {
        bug.Gene.CurrentGenePosition += (int) DestinationCell.CellType + 1;
        var neighbourBug = DestinationCell.LinkedBug;
        if (neighbourBug != null && neighbourBug.IsFriendBug(bug)) bug.Gene.CurrentGenePosition++;

        if (DestinationCell.CellType != CellEnum.TypeOfCell.Wall)
        {
            if (DestinationCell.CellType == CellEnum.TypeOfCell.Bamboo)
            {
                if (Data.Rnd.Next(0, 3) == 0)
                    DestinationCell.CellType = CellEnum.TypeOfCell.Poison;
                else
                    DestinationCell.CellType = CellEnum.TypeOfCell.Berry;
            }
            else
            {
                var pushDestination =
                    Coordinates.CoordinateShift[(bug.Direction + bug.Gene.genome[bug.Gene.NextGenePosition()]) % 8]
                    + DestinationCell.Coordinate;
                var pushDestinationCell = Data.WorldMap[pushDestination.Y, pushDestination.X];
                if (pushDestinationCell.CellType == CellEnum.TypeOfCell.Empty)
                {
                    pushDestinationCell.CellType = DestinationCell.CellType;
                    if (pushDestinationCell.CellType == CellEnum.TypeOfCell.Bug)
                    {
                        pushDestinationCell.LinkedBug = DestinationCell.LinkedBug;
                        pushDestinationCell.LinkedBug.CurrentPosition = pushDestination;
                        DestinationCell.LinkedBug = null;
                    }

                    DestinationCell.CellType = CellEnum.TypeOfCell.Empty;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Жук проверяет текущее количесвто своих жизней
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    private static bool CheckHealth(Bug bug)
    {
        if (bug.Health > bug.Gene.genome[bug.Gene.NextGenePosition()] * MaxBugHealth / Genome.LengthGenome)
            bug.Gene.CurrentGenePosition += 2;
        else
            bug.Gene.CurrentGenePosition += 3;

        return false;
    }

    /// <summary>
    /// Жук пытается атаковать содрежимое определённой клетки рядом с собой
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    private static bool Attack(Bug bug)
    {
        bug.Gene.CurrentGenePosition += (int) DestinationCell.CellType + 1;
        var neighbourBug = DestinationCell.LinkedBug;
        if (neighbourBug != null && neighbourBug.IsFriendBug(bug)) bug.Gene.CurrentGenePosition++;

        switch (DestinationCell.CellType)
        {
            case CellEnum.TypeOfCell.Mineral:
            {
                DestinationCell.CellType = CellEnum.TypeOfCell.MineralBerry;
                break;
            }

            case CellEnum.TypeOfCell.Bug:
            {
                if (DestinationCell.LinkedBug != null)
                {
                    DestinationCell.LinkedBug.Health -= 5;
                    bug.Health += 5;
                    bug.color = new Color(bug.color.r + 0.1f, bug.color.g - 0.1f, bug.color.b - 0.1f);
                }

                break;
            }

            case CellEnum.TypeOfCell.Wall:
            {
                break;
            }

            default:
            {
                DestinationCell.CellType = CellEnum.TypeOfCell.Empty;
                break;
            }
        }

        return true;
    }

    /// <summary>
    /// Жук пытается поделиться едой с содержимым определённой клетки рядом с собой
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    private static bool Share(Bug bug)
    {
        bug.Gene.CurrentGenePosition += (int) DestinationCell.CellType + 1;
        var neighbourBug = DestinationCell.LinkedBug;
        if (neighbourBug != null && neighbourBug.IsFriendBug(bug))
        {
            bug.Gene.CurrentGenePosition++;
            neighbourBug.Health += 5;
            bug.Health -= 5;
        }

        return false;
    }

    /// <summary>
    /// Команда находится в разработке из-за своей несбалансированности, пока жук может получать +2 к жизни, если применит команду к минералу
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    private static bool Photosynthesize(Bug bug)
    {
        bug.Gene.CurrentGenePosition += (int) DestinationCell.CellType + 1;
        var neighbourBug = DestinationCell.LinkedBug;
        if (neighbourBug != null && neighbourBug.IsFriendBug(bug)) bug.Gene.CurrentGenePosition++;

        if (DestinationCell.CellType == CellEnum.TypeOfCell.Mineral) bug.Health += 2;

        return true;
    }

    /// <summary>
    /// Выполняет прыжок по геному в зависимости от параметра
    /// </summary>
    /// <param name="bug"> Жук, который сейчас ходит  </param>
    private static bool GenomJump(Bug bug)
    {
        bug.Gene.CurrentGenePosition += bug.Gene.genome[bug.Gene.CurrentGenePosition];
        return false;
    }

    #endregion
}