using AMG.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AMG.AI.TasksWork.CommonTasks
{
    public class CardTask : ITaskWork
    {
        public int Points => GetCurrentProgress();

        private readonly List<CardTaskSwipeMemory> SwipesMemory = [];
        private readonly int Stages = 5;
        private readonly List<int> CorrectSwipes = [30, 30, 40, 70, 90];

        private readonly Random BrainRandomizer = new();

        public bool Execute()
        {
            var currentAttempt = new CardTaskSwipeMemory();
            bool allCorrect = true;

            for (int i = 0; i < Stages; i++)
            {
                int guessedStrong = 0;

                if (SwipesMemory.Count > 0)
                {
                    var lastAttempt = SwipesMemory.Last();

                    if (i < lastAttempt.SwipeData.Count)
                    {
                        var lastStageMemory = lastAttempt.SwipeData[i];

                        if (lastStageMemory.Points == 100)
                        {
                            guessedStrong = lastStageMemory.Strong;
                        }
                        else
                        {
                            int errorMargin = 100 - lastStageMemory.Points;
                            int direction = BrainRandomizer.Next(0, 2) == 0 ? 1 : -1;

                            guessedStrong = lastStageMemory.Strong + (direction * BrainRandomizer.Next(1, errorMargin + 5));

                            guessedStrong = Math.Clamp(guessedStrong, 0, 100);
                        }
                    }
                }
                else
                {
                    guessedStrong = BrainRandomizer.Next(0, 101);
                }

                int targetStrong = CorrectSwipes[i];

                int points = 100 - Math.Abs(targetStrong - guessedStrong);

                currentAttempt.SwipeData.Add(new CardTaskMemory
                {
                    Strong = guessedStrong,
                    Points = points
                });

                if (points < 100)
                {
                    allCorrect = false;
                }
            }

            SwipesMemory.Add(currentAttempt);

            return allCorrect;
        }
        private int GetCurrentProgress()
        {
            if (SwipesMemory.Count == 0) return 0;
            return SwipesMemory.Last().GetTotalPoints();
        }
    }

    public class CardTaskMemory
    {
        public int Strong = 0;
        public int Points = 0; // 100 = Correct
    }

    public class CardTaskSwipeMemory
    {
        public List<CardTaskMemory> SwipeData = [];

        public int GetTotalPoints()
        {
            int totalPoints = 0;
            foreach (CardTaskMemory data in SwipeData)
            {
                totalPoints += data.Points;
            }
            return totalPoints;
        }
    }
}