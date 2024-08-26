using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICurriculumAgent
{
   public void EndEpisodeCurriculum(float reward, bool interrupt = false);
}
