namespace SpecGurka.Interfaces;

public interface ISystemUserStoryService
{
    Task TestIfUserStoriesExistOnSystem(List<Feature> feature);
}
