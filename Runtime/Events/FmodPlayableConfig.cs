using FMOD;
using FMOD.Studio;
using FMODUnity;
using System;

namespace RoyTheunissen.FMODSyntax
{
    public abstract class FmodPlayableConfig
    {
        [NonSerialized] private GUID id;
        public string Id => id.ToString();

        /// <summary>
        /// Exposes this config's underlying event as an EventReference so it can be passed to APIs
        /// that expect one, such as FMODHelper.
        /// </summary>
        public EventReference Event => new EventReference { Guid = id };

        protected EventDescription EventDescription => RuntimeManager.GetEventDescription(id);

        public string Path
        {
            get
            {
                EventDescription.getPath(out string path);
                return path;
            }
        }

        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        public bool IsAssigned => !id.IsNull;

        public FmodPlayableConfig(string guid)
        {
            id = GUID.Parse(guid);
        }

        public void Preload()
        {
            EventDescription.loadSampleData();
        }

        public void Unload()
        {
            EventDescription.unloadSampleData();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}