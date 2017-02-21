using System;

namespace RStein.Async.Threading
{
  //Look at http://concurrencyfreaks.blogspot.cz/2013/12/left-right-classical-algorithm.html
  public class LeftRight<TInner>
  {
    private enum LeftRightChoice
    {
      None = 0,
      Left,
      Right
    }

    private readonly TInner m_leftInner;
    private readonly TInner m_rightInner;

    private readonly LeftRightVersion m_leftVersion;
    private readonly LeftRightVersion m_rightVersion;

    private LeftRightChoice m_innerChoice;
    private LeftRightChoice m_versionChoice;

    private readonly object m_writerLockRoot;

    public LeftRight(Func<TInner> innerFactory) : this()
    {
      m_leftInner = innerFactory();
      m_rightInner = innerFactory();
    }

    public LeftRight(TInner leftInner, TInner rightInner) : this()
    {
      if (leftInner == null)
      {
        throw new ArgumentNullException(nameof(leftInner));
      }

      if (rightInner == null)
      {
        throw new ArgumentNullException(nameof(rightInner));
      }

      m_leftInner = leftInner;
      m_rightInner = rightInner;
    }

    private LeftRight()
    {
      m_leftVersion = new LeftRightVersion();
      m_rightVersion = new LeftRightVersion();


      m_innerChoice = LeftRightChoice.Left;
      m_versionChoice = LeftRightChoice.Left;
      m_writerLockRoot = new Object();
    }

    public virtual TResult Read<TResult>(Func<TInner, TResult> reader)
    {
      if (reader == null)
      {
        throw new ArgumentNullException(nameof(reader));
      }

      var versionChoice = m_versionChoice;
      var version = getLeftRightVersion(versionChoice);
      version.Arrive();
      var innerInstance = getInnerObject();
      var result = reader(innerInstance);     
      version.Depart();
      return result;
    }

    public virtual TResult Write<TResult>(Func<TInner, TResult> writer)
    {
      lock (m_writerLockRoot)
      {        
        writeToInnerObject(writer);

        swapInstanceVersionChoice();

        waitForEmptyVersion();

        swapLeftRightVersionChoice();

        waitForEmptyVersion();

        return writeToInnerObject(writer);
      }
    }

    private void waitForEmptyVersion()
    {
      var currentVersionChoice = m_versionChoice;

      var toWaitVersion = getWaitVersion(currentVersionChoice);
      toWaitVersion.WaitForEmptyVersion();
    }

    private TResult writeToInnerObject<TResult>(Func<TInner, TResult> writer)
    {
      var innerInstance = getInnerObjectForWriter();
      return writer(innerInstance);
    }

    private void swapLeftRightVersionChoice()
    {
      m_versionChoice = (m_versionChoice == LeftRightChoice.Left) ?
                       LeftRightChoice.Right
                       : LeftRightChoice.Left;
    }

    private LeftRightVersion getWaitVersion(LeftRightChoice currentVersionChoice)
    {
      return getLeftRightVersion(currentVersionChoice == LeftRightChoice.Left ?
        LeftRightChoice.Right
        : LeftRightChoice.Left);
    }

    private void swapInstanceVersionChoice()
    {
      m_innerChoice = (m_innerChoice == LeftRightChoice.Left) ?
        LeftRightChoice.Right
        : LeftRightChoice.Left;
    }

    private TInner getInnerObjectForWriter()
    {
      return ((m_innerChoice == LeftRightChoice.Left) ? m_rightInner : m_leftInner);
    }

    private TInner getInnerObject()
    {
      return ((m_innerChoice == LeftRightChoice.Left) ? m_leftInner : m_rightInner);
    }

    private LeftRightVersion getLeftRightVersion(LeftRightChoice version)
    {
      return ((version == LeftRightChoice.Left) ? m_leftVersion : m_rightVersion);
    }
  }
}