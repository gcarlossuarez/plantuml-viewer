﻿/*
@startuml
   start
   :Let entSet be a set of Entitlements to revoke;
   :Add all dependent entitlements to entSet;
   :Delete all dependent entitlements from database;
   :Delete pools of entitlements 
   in entSet that are development pools;
   :Update consumed quantity of entSet;
   :Delete all entSet entitlements
    from database;
   :stackPools = filter Entitlements from entSet that
   have stacking_id attribute;
   partition for-each-entSet {
   :stackPool = find stack pool  
   for entitlement;
   :sSet = find all ents that have the 
   stacking_id;
   :Update stackPool based on sSet;
   }
   :virtEnts = filter Entitlements from entSet that 
   have virt_limit and are for distributors;
   partition for-each-virtEnts {
   if (virt_limit == unlimited) then
   -> YES;
   :Set bonus pool quantity to -1;
   else
   -> NO;
   :Add back reduced pool quantity;
   endif
   }
   :mEnts = get all modifier 
   entitlements of entSet entitlements;;
   :Lazily regenerate entitlement certificates 
    for all mEnts;
   :Compute compliance status for all 
   Consumers that have an entitlement in entSet;
   stop
   @enduml

 */
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

/*
 @startuml 
[*] --> State1
State1 --> [*]
State1 : this is a string
State1 : this is another string
State1 -> State2
State2 --> [*]
@enduml

 * */
