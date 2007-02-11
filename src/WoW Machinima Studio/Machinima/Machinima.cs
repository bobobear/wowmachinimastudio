using System;
using System.Collections.Generic;
using System.Text;
using WoW_Machinima_Studio.Game;
namespace WoW_Machinima_Studio.Machinima
{
	public class Action
	{
		//TODO Action class implemention
		public Action()
		{
			//TODO Action class constructor
			throw new Exception("The method or operation is not implemented.");
		}
	}
	public class ActionList : List<Action>
	{
		//TODO ActionList class implemention
		public ActionList()
		{
			//TODO Action class constructor
			throw new Exception("The method or operation is not implemented.");
		}
		
	}
	public class Sequence
	{
		//TODO Sequence class implemention
		private ActionList _actions;
		public ActionList Actions
		{
			get { return _actions; }
		}
		public Sequence()
		{
			//TODO Sequence class constructor
			throw new Exception("The method or operation is not implemented.");
		}
	}
	public class SequenceList : List<Sequence>
	{
		public SequenceList()
		{
			//TODO SequenceList class constructor
			throw new Exception("The method or operation is not implemented.");
		}
		//TODO SequenceList class implemention
	}
	public class Machinima
	{
		//TODO Machinima class implemention

		private SequenceList _sequences;
		public SequenceList Sequences
		{
			get { return _sequences; }
		}

		private UnitList _units;
		public UnitList Units
		{
			get { return _units; }
		}

		private GameObjectList _gameobjects;
		public GameObjectList GameObjects
		{
			get { return _gameobjects; }
		}

		public Machinima()
		{
			//TODO Machinima class constructor
			throw new Exception("The method or operation is not implemented.");
		}
		public void Save(String filename)
		{			
			throw new Exception("The method or operation is not implemented.");
		}
		public void Load(String filename)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public void Play() 
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
	public class MachinimaStudio
	{
		
		//TODO MachinimaStudio class implemention
		public MachinimaStudio()
		{
		}
		public void SetTimeOfDay(DateTime time)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public void UpdateWorld()
		{
			throw new Exception("The method or operation is not implemented.");
		}
		public void UpdateWorld(TimeSpan time)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
}
