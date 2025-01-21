# Master-Thesis

This is a software used in my Master's degree thesis "Multi-Agent Reinforcement Learning In Developement Of Patrolling Agents In Stealth Type Games"

https://apd.uw.edu.pl/diplomas/227849/

The work has used various MARL techniques in order to create a group of patrolling agents, that were used as a proof of concept of the usage of RL in games.

Feel free to use the code and contact / create an issue if you need anything.

## Some results

![ezgif com-video-to-gif-converter](https://github.com/user-attachments/assets/b49864fc-9f12-4853-bce1-3cb8eee707f4)

![wyniki2-ezgif com-video-to-gif-converter](https://github.com/user-attachments/assets/ce812611-8d3b-413f-ba9b-fe066e49870a)


<img style="float: left" src="https://github.com/user-attachments/assets/c36a7fad-5705-427b-9e4c-8179ed716755" width="25%">

<img style="float: left" src="https://github.com/user-attachments/assets/84b57749-c132-4352-a38a-b1e6a4c77f21" width="25%">

*Heat map of guards' routes*

## Preconditions

1. Mlagents installed with all dependencies (pytorch etc.)
2. Unity Engine installed
3. Repository's learn.py file copied to site-packages/mlagents/trainers/learn.py

## Contents

Overall there are some silliness in the code, stemming from the need to implement it fast. It's functional and I will not change it, but still you can use chunks of it in your own research.

### Scenes

Out of convinience certain experiments were divided into scenes.

- AllObjects: that scene contains all assets and components used during the project's course
- SingleCurriculum: contains a single-agent curriculum learning scenario for agents to learn traversing the maze. Works out of the box (just run it)
- MultiCurriculum : contains an acutual multi-agent curriculum scenario. It is advised to use the models pretrained with the single scenario
- Generalization: contains arenas used to test the generalization capabilities. No automatic mechanisms are present, so the user needs to set it up statically before the run

### Components

- Arena: Arena controller. Responsible for managing episodes' start and end. Grants episodic rewards and places entities procedurally at the episode's start
- CurriculumManager: Responsible for single-agent curriculum. Changes the current arena based on reward's EMA 
- CurriculumMetrics: Updates the custom training metrics for single-agent curriculum and communicates with mlagents module
- Guard: Basic guard agent controller
- GuardGroup: Mlagents guard group component wrapper for agents
- ICurriculumAgent: Agent interface
- ICurriculumManager: Curriculum manager interface
- ICurriculumMetrics: Curriculum metrics interface
- MultiAgentCurriculumManager: Responsible for single-agent curriculum. Changes the ratio of arenas based on EMA of change of rewards
- MultiAgentCurriculumMetrics: Updates the custom training metrics for multi-agent curriculum and communicates with mlagents module
- Thief: Basic thief agent controller 
- TrailManager: Used to gather the visual data used in creating the heatmaps

## Citation
If you use any of this software or the related work, I'd appreciate your citation 

@misc{korczynski-2024,
	author = {Korczyński, Konrad},
	month = {12},
	title = {{Multi-Agent reinforcement learning in developement of patrolling agents in stealth type games}},
	year = {2024},
}
